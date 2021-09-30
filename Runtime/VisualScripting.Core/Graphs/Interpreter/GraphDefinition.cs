using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// The runtime graph structure
    /// </summary>
    [Serializable]
    public class GraphDefinition
    {
        [Serializable]
        public struct VariableDescription
        {
            public BindingId Id;
            public string Name;
            public uint DataIndex;
            public ValueType Type;
            public VariableType VariableType;

            public VariableDescription(BindingId id, string name, uint dataIndex, ValueType type, VariableType variableType)
            {
                Id = id;
                Name = name;
                DataIndex = dataIndex;
                Type = type;
                VariableType = variableType;
            }
        }

        [Serializable]
        public struct VariableInitValue
        {
            public uint DataIndex;
            public Value Value;
        }

        [Serializable]
        public struct PortInfo
        {
            public bool IsDataPort; // True if this is a dataport, false if it's a trigger port
            public bool IsOutputPort; // True if this is an output port
            public bool IsOutputDataPort => IsDataPort && IsOutputPort;

            public uint DataOrTriggerIndex; // Index in the Data table for data port, and in the trigger table for trigger ports
            public NodeId NodeId; // The node ID owning that port
            public string PortName; // Port name (for debug only)

            public override string ToString()
            {
                string idxName = IsDataPort ? "DataIndex" : "TriggerIndex";
                return $"{idxName}: {DataOrTriggerIndex}, {nameof(NodeId)}: {NodeId}, {nameof(PortName)}: {PortName}";
            }
        }

        /// <summary>
        /// The node list itself
        /// </summary>
        [SerializeReference] public INode[] NodeTable = new INode[0];
        /// <summary>
        /// Contains info on each port. Indexed by Port.Index. Contains a sentinel value at index 0
        /// </summary>
        public PortInfo[] PortInfoTable = new PortInfo[0];

        /// <summary>
        /// Contains the index of the PortInfo of each output data port. Indexed by PortInfo.DataOrTriggerIndex. Contains a sentinel value at index 0
        /// </summary>
        public int[] DataPortTable = new int[0];

        public List<VariableDescription> Variables = new List<VariableDescription>();
        public List<VariableInitValue> VariableInitValues = new List<VariableInitValue>();
        // Used for reflection nodes
        // TODO Make it serializable. Member is only serializable with fsserializer right now
        public List<ReflectedMember> ReflectedMembers = new List<ReflectedMember> { default };
        public RuntimeGraphAsset[] GraphReferences;

        public uint ComputeHash()
        {
            uint hash = 0;
            hash = HashUtility.HashCollection(NodeTable, (node, u) => node == null ? 0 : UnsafeUtility.IsUnmanaged(node.GetType()) ? HashUtility.HashBoxedUnmanagedStruct(node, u) : HashUtility.HashManaged(node, u), hash);
            hash = HashUtility.HashCollection(PortInfoTable, HashUtility.HashManaged, hash);
            hash = HashUtility.HashCollection(DataPortTable, HashUtility.HashUnmanagedStruct, hash);
            hash = HashUtility.HashCollection(Variables, HashUtility.HashManaged, hash);
            hash = HashUtility.HashCollection(VariableInitValues, HashUtility.HashUnmanagedStruct, hash);
            return hash;
        }

        public bool HasConnectedValue(IPort port)
        {
            if (port is IInputTriggerPort)
                throw new NotImplementedException();

            return PortInfoTable[(int)port.GetPort().Index].DataOrTriggerIndex != 0;
        }

        public bool AreConnected(IOutputDataPort output, IInputDataPort input)
        {
            return PortInfoTable[(int)output.GetPort().Index].DataOrTriggerIndex == PortInfoTable[(int)input.GetPort().Index].DataOrTriggerIndex;
        }

        public bool AreConnected(IOutputTriggerPort output, IInputTriggerPort input)
        {
            return PortInfoTable[(int)output.GetPort().Index].DataOrTriggerIndex == PortInfoTable[(int)input.GetPort().Index].DataOrTriggerIndex;
        }

        internal string GraphDump()
        {
            var result = new List<string>
            {
                " Graph Dump",
                "------------",
                $"Number of nodes    : {NodeTable.Length}",
                $"Number of ports    : {PortInfoTable.Length - 1}", // -1 because Port 0 is NULL
                $"Number of data     : {DataPortTable.Length - 1}", // -1 because Data 0 is NULL
                "",
                "NODES"
            };

            result.AddRange(NodeTable.Select((t, i) => $"Node {i} => {t.GetType()}"));
            result.Add("");
            result.Add("PORT TABLE");

            for (int i = 0; i < PortInfoTable.Length; i++)
            {
                var portInfo = PortInfoTable[i];
                var slotType = portInfo.IsDataPort ? "Data" : "Trigger";
                var slotDir = portInfo.IsOutputPort ? "Output" : "Input";
                var str =
                    $"{slotType} {slotDir} Port({i}, {portInfo.PortName}), belongs to Node {portInfo.NodeId.GetIndex()}";

                if (portInfo.IsDataPort)
                    str += portInfo.DataOrTriggerIndex == 0
                        ? " <UNCONNECTED PORT>"
                        : $", uses {slotType} slot {portInfo.DataOrTriggerIndex}";
                else if (portInfo.IsOutputPort)
                {
                    str += portInfo.DataOrTriggerIndex == 0 ? " <UNCONNECTED PORT>" : " Port(s) to trigger on execution: ";
                    var triggerIndex = (int)portInfo.DataOrTriggerIndex;
                    str += $"{triggerIndex}, ";
                }

                result.Add(str);
            }

            result.Add("");

            return string.Join("\n\r", result.ToArray());
        }
    }
}
