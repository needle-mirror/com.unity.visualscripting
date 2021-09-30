using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    public partial class GraphBuilder
    {
        HashSet<INode> m_AddedNodes = new HashSet<INode>();
        internal bool NodeHasBeenAdded(INode node) => m_AddedNodes.Contains(node);
        internal INode AddNodeInternal(NodeId id, INode node, PortMapper mapper, IUnit nodeModel = null)
        {
            Assert.IsTrue(m_AddedNodes.Add(node), "Node has already been added to the GraphBuilder");
            Assert.IsNotNull(mapper, $"Null {nameof(mapper)} for node {node}");

            // Add the node to the definition
            NodeTable.Add(id, (node, mapper));
            PortToNodeId.AddRange(mapper.AllPorts.Select(p =>
                new KeyValuePair<uint, NodeId>(p.Value.PortIndex, id)));
            if (nodeModel != null)
                _nodeMapping.Add(id, nodeModel);
            return node;
        }

        public void RemoveNode(NodeId oldNodeId, INode oldNode)
        {
            foreach (var fieldInfo in FlowGraphTranslator.GetNodePorts(oldNode.GetType()))
            {
                IPort port = (IPort)fieldInfo.GetValue(oldNode);
                var portIndex = port.GetPort().Index;
                if (portIndex == 0)
                    continue;
                var portLastIndex = port.GetLastIndex();
                // int deletedEdgeCount =
                m_EdgeTable.RemoveAll(e => (e.InputPortIndex >= portIndex && e.InputPortIndex <= portLastIndex) || (e.OutputPortIndex >= portIndex && e.OutputPortIndex < portLastIndex));
                // Debug.Log($"Deleted {deletedEdgeCount} edges when removing node {oldNodeId} {oldNode}");
            }

            _nodeMapping.Remove(oldNodeId);
            NodeTable.Remove(oldNodeId);
            m_NodeAnnotations.Remove(oldNodeId);
        }

        private void AssertPortMappingIsValid(KeyValuePair<IUnitPort, PortMapper.MappedPort> mappedPort, IPort port, Dictionary<uint, PortBuildingInfo> portBuildingInfos)
        {
            // TODO: still valid ?
            // Assert.AreEqual(portBuildingInfos.Count + 1, mappedPort.Value.PortIndex);

            Assert.AreNotEqual(0, port.GetPort().Index,
                $"Port {mappedPort.Value.PortName} has an invalid index: {port}. Call graphBuilder.SetupPort() to assign a valid index");
        }

        public void AddNodeFromModel(IUnit nodeModel, INode node, PortMapper portToOffsetMapping)
        {
            Assert.IsNotNull(portToOffsetMapping);
            // TODO find unit descriptor ?
            var nodeId = GetNextNodeId();
            AddNodeInternal(nodeId, node, portToOffsetMapping, nodeModel);
        }

        public PortMapper AutoAssignPortIndicesAndMapPorts(IUnit unit, INode node)
        {
            var runtimeType = node.GetType();
            PortMapper mapping = new PortMapper();
            var excludedPorts = runtimeType.GetAttribute<NodeDescriptionAttribute>()?.UnmappedPorts;
            Dictionary<(string, PortDirection), IUnitPort> ports = new Dictionary<(string, PortDirection), IUnitPort>();
            ports.AddRange(unit.ports
                .Where(p => excludedPorts == null || !excludedPorts.Contains(p.key))
                .Select(p =>
                    new KeyValuePair<(string, PortDirection), IUnitPort>(
                        (p.key, p is IUnitInputPort ? PortDirection.Input : PortDirection.Output), p)));
            var fieldInfos = FlowGraphTranslator.GetNodePorts(runtimeType).ToArray();

            /*
             * cases, according to Authoring Port/Runtime Port naming:
             * - identical: trivial
             * - case difference:
             *   - no conflicts: trivial
             *   - conflict: warn the user, require a [PortDescription("authoring port name")] attribute on the runtime port
             *
             */

            foreach (var fieldInfo in fieldInfos)
            {
                string fieldInfoName;
                bool nameComesFromAttribute = false;
                if (Attribute.IsDefined(fieldInfo, typeof(PortDescriptionAttribute)))
                {
                    fieldInfoName = fieldInfo.GetAttribute<PortDescriptionAttribute>().AuthoringPortName;
                    nameComesFromAttribute = true;
                }
                else
                    fieldInfoName = fieldInfo.Name;

                var fieldDirection = typeof(IInputPort).IsAssignableFrom(fieldInfo.FieldType)
                    ? PortDirection.Input
                    : PortDirection.Output;

                if (typeof(IMultiPort).IsAssignableFrom(fieldInfo.FieldType) && unit is IMultiInputUnit multiInputUnit)
                {
                    IMultiPort port = (IMultiPort)fieldInfo.GetValue(node);
                    port.SetCount(multiInputUnit.inputCount);
                    mapping.AddMultiPortIndexed(this, i => multiInputUnit.multiInputs[i], ref port);
                    for (int i = 0; i < port.GetDataCount(); i++)
                        Assert.IsTrue(ports.Remove((i.ToString(), fieldDirection)));
                    fieldInfo.SetValue(node, port);
                }
                else
                {
                    if (!FlowGraphTranslator.GetMatchingAuthoringPort(ports, runtimeType, fieldInfoName, fieldDirection,
                        nameComesFromAttribute, fieldInfo, out var unitPort))
                        continue;

                    Assert.IsTrue(ports.Remove((unitPort.key, fieldDirection)));

                    IPort port = (IPort)fieldInfo.GetValue(node);
                    mapping.AddSinglePort(this, unitPort, ref port);
                    fieldInfo.SetValue(node, port);
                }
            }

            foreach (var remainingPort in ports)
            {
                if (remainingPort.Value is IUnitControlPort && node is IDataNode)
                    continue;
                Debug.LogError($"Remaining port: {unit.GetType()}.{remainingPort.Key}");
            }

            return mapping;
        }

        /// <summary>
        /// Assigns all ports of the node and add them all to the mapper. Any port present in the dictionary will be
        /// mapped to the unit port there. The dictionary can contain only some ports.
        /// </summary>
        /// <param name="inode"></param>
        /// <param name="mapping"></param>
        /// <param name="portModelToRuntimeField"></param>
        /// <returns></returns>
        [MustUseReturnValue]
        public INode AutoAssignPortIndicesAndMapPorts(INode inode, PortMapper mapping,
            Dictionary<IUnitPort, FieldInfo> portModelToRuntimeField = null)
        {
            foreach (var fieldInfo in FlowGraphTranslator.GetNodePorts(inode.GetType()))
            {
                IPort port = (IPort)fieldInfo.GetValue(inode);
                // var metadata = BaseDotsNodeModel.GetPortMetadata(inode, fieldInfo);

                // this takes care of multiports and will add one entry per multiport port
                // Value? defaultValue = null;// metadata.DefaultValue != null ? (Value?)ValueFromTypeAndObject(metadata.Type, metadata.DefaultValue) : null;
                if (port is IMultiPort multiPort)
                {
                    var portModels = portModelToRuntimeField?
                        .Where(x => x.Value == fieldInfo)?
                            .Select(x => x.Key)?
                            .ToList();
                    mapping.AddMultiPort(this, portModels != null && portModels.Any() ? portModels : null, ref multiPort);
                    port = multiPort;
                }
                else
                    mapping.AddSinglePort(this, portModelToRuntimeField?.FirstOrDefault(x => x.Value == fieldInfo).Key, ref port);
                fieldInfo.SetValue(inode, port);
            }

            return inode;
        }

        internal IUnit GetSourceNodeModel(NodeId nodeId)
        {
            return _nodeMapping.TryGetValue(nodeId, out var unit) ? unit : null;
        }
    }
}
