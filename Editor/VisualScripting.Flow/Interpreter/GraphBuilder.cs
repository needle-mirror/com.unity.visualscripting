using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    public partial class GraphBuilder
    {
        public class BoltCompilationResult
        {
            public GraphDefinition GraphDefinition = new GraphDefinition();
        }

        public struct VariableHandle
        {
            public uint DataIndex;

            public VariableHandle(uint dataIndex) => DataIndex = dataIndex;
        }

        struct PortBuildingInfo
        {
            public uint OutputTriggerEdgeCount;
            public bool IsDataPort; // True if this is a dataport, false if it's a trigger port
            public bool IsOutputPort; // True if this is an output port
            public uint DataIndex; // Index in the Data table for data port
            public uint TriggerIndex; // Index in the trigger table for trigger ports
            public NodeId NodeId; // The node ID owning that port
            public string PortName; // Port name (for debug only)
            public Value? DefaultValue;
        }

        internal struct Edge
        {
            public uint OutputPortIndex;
            public uint InputPortIndex;
        }

        public readonly Dictionary<NodeId, (INode node, PortMapper mapper)> NodeTable = new Dictionary<NodeId, (INode node, PortMapper mapper)>();

        /// <summary>
        /// Used during edge creation to get the port indices and to create the debug symbols to map NodeIds back to node models for tracing
        /// </summary>
        Dictionary<NodeId, IUnit> _nodeMapping;

        /// <summary>
        /// Used by GraphTransform to find a port's owner node
        /// </summary>
        internal readonly Dictionary<uint, NodeId> PortToNodeId;

        /// <summary>
        /// indices in PortInfoTable. used so we can map ports in an arbitrary order, and they'll get the right data index later
        /// </summary>
        internal List<Edge> m_EdgeTable;

        uint _lastPortIndex;
        uint _nextNodeId;

        Dictionary<BindingId, VariableHandle> m_VariableToDataIndex;
        Dictionary<UnifiedVariableUnit, BindingId> m_VariableUnitsToBindingIds;

        Dictionary<Member, uint> m_ReflectedMembers = new Dictionary<Member, uint>();

        private Dictionary<FlowGraph, RuntimeGraphAsset> m_TranslatedGraphs =
            new Dictionary<FlowGraph, RuntimeGraphAsset>();
        private Dictionary<SubgraphUnit, (RuntimeGraphAsset, ScriptGraphAssetIndex)> m_GraphReferences =
            new Dictionary<SubgraphUnit, (RuntimeGraphAsset, ScriptGraphAssetIndex)>();

        BoltCompilationResult m_Result;

        private List<IUnit> m_UnitsToCodegen;

        internal RuntimeGraphDebugData.NodeAnnotations m_NodeAnnotations;
        public IEnumerable<IUnit> UnitsToCodegen => m_UnitsToCodegen ?? Enumerable.Empty<IUnit>();
        public void AddUnitToCodegen(IUnit unit) => (m_UnitsToCodegen = m_UnitsToCodegen ?? new List<IUnit>()).Add(unit);

        public GraphBuilder(FlowGraphContext mContext, TranslationOptions options = TranslationOptions.Default)
        {
            m_Context = mContext;
            Options = options;
            m_Result = new BoltCompilationResult();
            _nodeMapping = new Dictionary<NodeId, IUnit>();
            m_EdgeTable = new List<Edge>();
            PortToNodeId = new Dictionary<uint, NodeId>();
            m_VariableToDataIndex = new Dictionary<BindingId, VariableHandle>();
            m_VariableUnitsToBindingIds = new Dictionary<UnifiedVariableUnit, BindingId>();
        }

        public TranslationOptions Options { get; }

        // public void AddError(string description, IGTFNodeModel nodeModel)
        // {
        //     m_Result.AddError(description, nodeModel);
        // }
        //
        // public void AddWarning(string description, IGTFNodeModel nodeModel)
        // {
        //     m_Result.AddWarning(description, nodeModel);
        // }

        public NodeId GetNextNodeId() => new NodeId(_nextNodeId++);

        public void AssignPortIndex(INode node, FieldInfo fieldInfo, out PortDirection direction,
            out PortType type, out string name)
        {
            name = fieldInfo.Name;

            var port = (IPort)fieldInfo.GetValue(node);

            AssignPortIndex(out direction, out type, ref port);

            fieldInfo.SetValue(node, port);
        }

        public void AssignPortIndex<TPort>(ref TPort port) where TPort : IPort => AssignPortIndex(out _, out _, ref port);

        private void AssignPortIndex<TPort>(out PortDirection direction, out PortType type, ref TPort port) where TPort : IPort
        {
            var portIndex = _lastPortIndex + 1;

            _lastPortIndex += (uint)port.GetDataCount();

            var internalPort = port.GetPort();
            internalPort.Index = portIndex;

            direction = port.GetDirection();
            type = port.GetPortType();
            port.SetPort(internalPort);
        }

        private uint nextDataIndex;
        private FlowGraphContext m_Context;

        public uint AllocateDataIndex()
        {
            return ++nextDataIndex;
        }

        /// <summary>
        /// Builds the runtime graph from everything added to the builder. It guarantees node ids and port ids will be
        /// sequential, even if nodes/ports have been removed.
        /// </summary>
        /// <returns></returns>
        public BoltCompilationResult Build()
        {
            var definition = m_Result.GraphDefinition;

            // add reflected members with a sentinel value. TODO: do that at the end and strip unused members
            m_Result.GraphDefinition.ReflectedMembers = Enumerable.Repeat(default(ReflectedMember), m_ReflectedMembers.Count + 1).ToList();
            foreach (var keyValuePair in m_ReflectedMembers)
                m_Result.GraphDefinition.ReflectedMembers[(int)keyValuePair.Value] = new ReflectedMember(keyValuePair.Key);

            m_Result.GraphDefinition.GraphReferences = new RuntimeGraphAsset[m_GraphReferences.Count + 1];
            foreach (var graphReference in m_GraphReferences)
            {
                m_Result.GraphDefinition.GraphReferences[graphReference.Value.Item2.Index] = graphReference.Value.Item1;
            }

            definition.NodeTable = new INode[NodeTable.Count];

            // remappings for node/port ids
            Dictionary<NodeId, NodeId> nodeIdRemapping = new Dictionary<NodeId, NodeId>();
            Dictionary<uint, uint> portIdRemapping = new Dictionary<uint, uint>();

            Dictionary<uint, PortBuildingInfo> portInfos = new Dictionary<uint, PortBuildingInfo>();

            uint i = 0;
            foreach (var nodeEntry in NodeTable.OrderBy(x => x.Key.GetIndex()))
            {
                var newNodeId = new NodeId(i);
                var mapper = nodeEntry.Value.mapper;

                nodeIdRemapping.Add(nodeEntry.Key, newNodeId);

                definition.NodeTable[i] = nodeEntry.Value.node;

                var allNodePorts = FlowGraphTranslator.GetNodePorts(nodeEntry.Value.node.GetType())
                    .Select(f => (((IPort)f.GetValue(nodeEntry.Value.node)).GetPort().Index, f))
                    .Where(f => f.Index != 0) // unused ports
                    .ToDictionary(f => f.Index, f => f.f);

                foreach (var mappedPort in mapper.AllPorts)
                {
                    PortMapper.MappedPort mappedPortValue = mappedPort.Value;

                    // here: remap port indices, make them sequential. find the port field in the node and change it, store the change to patch edges
                    // if the port is a multi port, this is the id of the multiport (eg. a multiport 3,4,5: this is 3)
                    uint oldPortId = mappedPortValue.Port.GetPort().Index;
                    // new sequential port index
                    uint newPortId = (uint)portInfos.Count + 1;

                    // multiports will fill the remapping of all their ports, eg. if a multiport 3,4,5 is remapped to 2,3,4,
                    // this will fill 3->2,4->3,5->4
                    if (!portIdRemapping.TryGetValue(mappedPortValue.PortIndex, out var newPortIndex))
                    {
                        newPortIndex = (uint)(portInfos.Count + 1);

                        IPort port = mappedPortValue.Port;
                        // as the port is boxed, we need to set this only once for a multiport
                        port.SetPort(new Port { Index = newPortId });
                        allNodePorts[oldPortId].SetValue(nodeEntry.Value.node, port);

                        for (int j = 0; j < port.GetDataCount(); j++)
                        {
                            portIdRemapping.Add((uint)(mappedPortValue.PortIndex + j), (uint)(newPortIndex + j));
                        }
                    }


                    mappedPortValue.PortIndex = newPortIndex;

                    AssertPortMappingIsValid(mappedPort, mappedPortValue.Port, portInfos);

                    // mapped ports already contains one entry per multiport port. eg. if a log node has two message inputs,
                    // the mapper will contain an entry "Messages_0, port.index = 3, portIndex = 3" and one
                    // "Messages_1, port.index = 3, portIndex = 4" so no need to iterate on port.GetDataCount()

                    bool isDataPort = mappedPortValue.Type == PortType.Data;
                    bool isOutputPort = mappedPortValue.Port.GetDirection() == PortDirection.Output;
                    var newPortInfo = new PortBuildingInfo
                    {
                        IsDataPort = isDataPort,
                        IsOutputPort = isOutputPort,
                        NodeId = newNodeId,
                        PortName = mappedPortValue.PortName,
                        DefaultValue = mappedPortValue.DefaultValue
                    };
                    portInfos.Add(mappedPortValue.PortIndex, newPortInfo);
                }
                i++;
            }


            SetupNodesDataIndices();

            // for (int i = 0; i < m_Result.GraphDefinition.Variables.Count; i++)
            // {
            //     if (m_Result.GraphDefinition.Variables[i].VariableType == VariableType.Input)
            //     {
            //         // if count == 0, this is the first variable of that type
            //         if (m_Result.GraphDefinition.Inputs.Count == 0)
            //             m_Result.GraphDefinition.Inputs.StartIndex = i;
            //         m_Result.GraphDefinition.Inputs.Count++;
            //     }
            //
            //     if (m_Result.GraphDefinition.Variables[i].VariableType == VariableType.Output)
            //     {
            //         // if count == 0, this is the first variable of that type
            //         if (m_Result.GraphDefinition.Outputs.Count == 0)
            //             m_Result.GraphDefinition.Outputs.StartIndex = i;
            //         m_Result.GraphDefinition.Outputs.Count++;
            //     }
            // }

            // Count the number of output edge for each output trigger port
            for (var index = 0; index < m_EdgeTable.Count; index++)
            {
                var edge = m_EdgeTable[index];
                if (portIdRemapping.TryGetValue(edge.InputPortIndex, out var remappedInput))
                    edge.InputPortIndex = remappedInput;
                if (portIdRemapping.TryGetValue(edge.OutputPortIndex, out var remappedOutput))
                    edge.OutputPortIndex = remappedOutput;
                m_EdgeTable[index] = edge;

                if (!portInfos.TryGetValue(edge.OutputPortIndex, out var outputPortInfo))
                {
                    Debug.LogWarning($"Unknown port from edge {edge.OutputPortIndex} -> {edge.InputPortIndex}, deleting edge");
                    m_EdgeTable.RemoveAt(index);
                    index--;
                    continue;
                }

                // Count the number of output edge for each output trigger port
                if (!outputPortInfo.IsDataPort)
                {
                    outputPortInfo.OutputTriggerEdgeCount++;
                    portInfos[edge.OutputPortIndex] = outputPortInfo;
                }
            }

            var connectedOutputDataPorts = new HashSet<uint>();
            // Process the edge table
            var dataPortTable = new List<int>((int)nextDataIndex + 1);
            foreach (var edge in m_EdgeTable)
            {
                // Retrieve the input & output port info
                var outputPortInfo = portInfos[edge.OutputPortIndex];
                var inputPortInfo = portInfos[edge.InputPortIndex];
                Assert.AreEqual(outputPortInfo.IsDataPort, inputPortInfo.IsDataPort,
                    "Only ports of the same kind (trigger or data) can be connected");
                if (outputPortInfo.IsDataPort)
                {
                    Assert.IsTrue(outputPortInfo.NodeId.IsValid());
                    // For data port, copy the DataIndex of the output port in the dataindex of the input port &
                    // Keep track of the output node (because we will pull on it & execute it)
                    // TODO: Optim opportunity here: We could detect flownode & constant here & avoid runtime checks by cutting link
                    inputPortInfo.DataIndex = outputPortInfo.DataIndex;
                    // mark which node to execute when pulling on a data input port

                    // pad list. sometimes a dataindex has been allocated, but stripped because the port was not
                    // connected in the end. rather than creating the list wth nextDataIndex + 1 items and having extra
                    // empty entries there, add them on demand here
                    while (dataPortTable.Count <= inputPortInfo.DataIndex)
                        dataPortTable.Add(default);
                    dataPortTable[(int)inputPortInfo.DataIndex] = (int)edge.OutputPortIndex;

                    portInfos[edge.InputPortIndex] = inputPortInfo;
                    connectedOutputDataPorts.Add(edge.OutputPortIndex);
                }
                else
                {
                    outputPortInfo.TriggerIndex = edge.InputPortIndex;
                    portInfos[edge.OutputPortIndex] = outputPortInfo;
                }
            }

            definition.DataPortTable = dataPortTable.ToArray();

            definition.PortInfoTable = new GraphDefinition.PortInfo[(portInfos.Any() ? portInfos.Max(p => p.Key) : 0) + 1];
            foreach (var portBuildingInfo in portInfos)
            {
                var portInfo = new GraphDefinition.PortInfo
                {
                    NodeId = portBuildingInfo.Value.NodeId,
                    PortName = portBuildingInfo.Value.PortName,
                    IsDataPort = portBuildingInfo.Value.IsDataPort,
                    IsOutputPort = portBuildingInfo.Value.IsOutputPort,
                    DataOrTriggerIndex = portBuildingInfo.Value.IsDataPort
                        ? portBuildingInfo.Value.DataIndex
                        : portBuildingInfo.Value.TriggerIndex,
                };
                // Reset DataIndex of outputDataPorts not connected and not bound to a variable
                if (portInfo.IsOutputDataPort)
                {
                    var isConnected = connectedOutputDataPorts.Contains(portBuildingInfo.Key);
                    var isVariablePort = m_VariableToDataIndex.Any(h => h.Value.DataIndex == portInfo.DataOrTriggerIndex);
                    if (!isConnected && !isVariablePort)
                        portInfo.DataOrTriggerIndex = 0;
                }
                definition.PortInfoTable[portBuildingInfo.Key] = portInfo;
            }

            _nodeMapping = _nodeMapping.ToDictionary(x => nodeIdRemapping.TryGetValue(x.Key, out var newId) ? newId : x.Key, x => x.Value);
            m_NodeAnnotations.RemapNodeIds(nodeIdRemapping);

            return m_Result;

            void SetupNodesDataIndices()
            {
                // skip sentinel value, but include Count
                for (uint index = 1; index <= portInfos.Count; index++)
                {
                    var portInfo = portInfos[index];
                    if (portInfo.IsDataPort && portInfo.IsOutputPort)
                    {
                        // TODO check if that still makes sense. I think they can't get overriden anymore as variables store a variable handle now
                        if (portInfo.DataIndex == 0) // if it hasn't been overriden
                        {
                            var info = portInfo;
                            info.DataIndex = AllocateDataIndex();
                            if (info.DefaultValue.HasValue)
                                AddVariableInitValue(info.DataIndex, info.DefaultValue.Value);
                            portInfos[index] = info;
                        }

                        // this would be the place to store port default values in graph definition's VariableInitValues
                    }
                }
            }
        }

        [MenuItem("internal:Visual Scripting/Clear runtime graphs", false)]
        static void ClearRuntimeGraphs(MenuCommand menuCommand)
        {
            foreach (var flowMacro in Resources.FindObjectsOfTypeAll<ScriptGraphAsset>())
            {
                if (flowMacro?.graph?.RuntimeGraphAsset)
                {
                    AssetDatabase.RemoveObjectFromAsset(flowMacro.graph.RuntimeGraphAsset);
                    ScriptableObject.DestroyImmediate(flowMacro.graph.RuntimeGraphAsset);
                    flowMacro.graph.RuntimeGraphAsset = null;
                }
            }
        }

        public uint AddReflectedMember(Member member)
        {
            if (m_ReflectedMembers.TryGetValue(member, out var idx))
                return idx;
            idx = (uint)m_ReflectedMembers.Count + 1;
            m_ReflectedMembers.Add(member, idx);
            return idx;
        }

        internal Member GetReflectedMember(uint memberIndex)
        {
            return m_ReflectedMembers.FirstOrDefault(x => x.Value == memberIndex).Key;
        }

        public RuntimeGraphDebugData CreateDebugData()
        {
            Dictionary<NodeId, Guid> nodes = _nodeMapping.Where(x => x.Value != null)
                .ToDictionary(x => x.Key, x => x.Value.guid);
            Dictionary<uint, IGraphItem> ports = new Dictionary<uint, IGraphItem>();
            foreach (var node in NodeTable)
            {
                foreach (var keyValuePair in node.Value.mapper.AllPorts.Where(x => x.Key != null))
                {
                    try
                    {
                        ports.Add(keyValuePair.Value.PortIndex, keyValuePair.Key);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Duplicate key: {keyValuePair.Value.PortIndex} for unit port {keyValuePair.Key} of {keyValuePair.Key.unit}\n\n{e}");
                    }
                }
            }
            var debugData = new RuntimeGraphDebugData(nodes, ports, m_NodeAnnotations);
            return debugData;
        }

        public ScriptGraphAssetIndex GetTranslatedGraphAsset(SubgraphUnit superUnit)
        {
            RuntimeGraphAsset graphRuntimeGraphAsset;

            // don't translate if it's already done
            if (!m_TranslatedGraphs.TryGetValue(superUnit.nest.graph, out graphRuntimeGraphAsset))
            {
                m_Context.NestedContext(superUnit).Translate();
                graphRuntimeGraphAsset = superUnit.nest.graph.RuntimeGraphAsset;
                m_TranslatedGraphs.Add(superUnit.nest.graph, graphRuntimeGraphAsset);
            }

            // still get a new index for the super unit - we' ll need one instance of the graph per superunit node, we can' t reuse the same one
            var newIndex = new ScriptGraphAssetIndex { Index = (m_GraphReferences.Count + 1) };
            m_GraphReferences.Add(superUnit, (graphRuntimeGraphAsset, newIndex));
            return newIndex;
        }
    }
}
