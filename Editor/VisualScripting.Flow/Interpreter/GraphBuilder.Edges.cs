using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    public partial class GraphBuilder
    {
        public void CreateEdge(OutputDataPort outputPort, IUnitPort inputPortModel)
        {
            if (!GetPortIndex(inputPortModel, out var inputPortIndex))
                return;
            CreateEdge(outputPort, inputPortIndex);
        }

        public void CreateEdge(IUnitPort outputPortModel, IUnitPort inputPortModel)
        {
            if (outputPortModel == null || inputPortModel == null)
                return;

            if (GetPortIndex(inputPortModel, out var inputPortIndex))
            {
                if (GetPortIndex(outputPortModel, out var outputPortIndex))
                {
                    CreateEdge(outputPortIndex, inputPortIndex);
                }
            }
        }

        /// <summary>
        /// Gets the port index, in graph space (each port has a unique index graph-wide, eg. a node might have ports 5,6,7)
        /// </summary>
        /// <param name="portModel"></param>
        /// <param name="portIndex"></param>
        /// <returns></returns>
        private bool GetPortIndex(IUnitPort portModel, out IPort portIndex)
        {
            portIndex = default;
            foreach (var entry in _nodeMapping.Where(x => x.Value == portModel.unit))
            {
                var mapping = NodeTable[entry.Key].mapper;
                if (mapping.TryGetPortIndexOfPortModel(portModel, out portIndex))
                    return true;
            }

            Debug.LogError($"Cannot resolve port for portmodel {(portModel.unit is MemberUnit memberUnit ? memberUnit.member.ToString() : portModel.unit.ToString())}.{portModel.key}");
            return false;
        }

        internal void CreateEdge(IPort outputPort, IPort inputPort)
        {
            Assert.AreNotEqual(0, outputPort.GetPort().Index);
            Assert.AreNotEqual(0, inputPort.GetPort().Index);
            Assert.IsTrue(outputPort is IOutputPort);
            Assert.IsTrue(inputPort is IInputPort);
            Assert.AreEqual(outputPort.IsData(), inputPort.IsData(),
                "Only ports of the same kind (trigger or data) can be connected");

            // Debug.Log($"Create Edge {outputPortIndex}:{outputPortInfo.PortName} -> {inputPortIndex}:{inputPortInfo.PortName}");
            if (outputPort.IsTrigger())
                Assert.IsFalse(m_EdgeTable.Any(e => e.OutputPortIndex == outputPort.GetPort().Index), "trigger already connected");
            m_EdgeTable.Add(new Edge { OutputPortIndex = outputPort.GetPort().Index, InputPortIndex = inputPort.GetPort().Index });
        }
    }
}
