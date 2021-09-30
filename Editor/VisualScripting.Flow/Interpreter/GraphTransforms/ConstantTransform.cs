using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    class ConstantTransform : GraphTransform
    {
        Dictionary<uint, Value> m_Values = new Dictionary<uint, Value>();

        protected override void DoVisit(NodeId nodeId, INode n, PortMapper nodeMapper)
        {
            if (n is IConstantNode constantNode)
            {
                m_Values.Clear();
                var ctx = new DataGraphInstance(m_Values, GetOutputPortToConnectedInputPortsLookup(), m_Builder);
                constantNode.Execute(ctx);
                foreach (var value in m_Values)
                {
                    Debug.Log($"{value.Key}: {value.Value}");
                }

                // m_Builder.RemoveNode(nodeId, constantNode);
            }
        }
    }
}
