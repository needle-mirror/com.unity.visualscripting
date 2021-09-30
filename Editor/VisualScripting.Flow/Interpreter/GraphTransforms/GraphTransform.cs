using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    public abstract class GraphTransform
    {
        protected GraphBuilder m_Builder;
        private ILookup<uint, uint> m_OutputPortToConnectedInputPorts;
        private HashSet<NodeId> _nodesToVisit;

        public void Run(GraphBuilder builder)
        {
            FlowGraphTranslator.InitCache();
            m_Builder = builder;

            _nodesToVisit = new HashSet<NodeId>();
            DoRun();

            _nodesToVisit = null;
            m_Builder = null;
        }

        protected virtual void DoRun()
        {
            var nodeTableKeys = m_Builder.NodeTable.Keys.ToArray();
            foreach (var nodeId in nodeTableKeys)
            {
                Visit(nodeId);
            }
        }

        protected void ClearVisitedNodes() => _nodesToVisit.Clear();

        protected void Visit(NodeId nodeId)
        {
            if (_nodesToVisit.Add(nodeId))
            {
                var node = m_Builder.NodeTable[nodeId];
                // Debug.Log($"Visit {nodeId} {node}");
                DoVisit(nodeId, node.node, node.mapper);
            }
            // else
            // Debug.Log($"    Skip {nodeId}");
        }

        protected abstract void DoVisit(NodeId nodeId, INode n, PortMapper nodeMapper);

        // TODO inelegant
        protected IEnumerable<(NodeId id, IInputDataPort port)> GetConnectedPort(IOutputDataPort port)
        {
            foreach (var edge in m_Builder.m_EdgeTable
                     .Where(e => e.OutputPortIndex >= port.GetPort().Index && e.OutputPortIndex < port.GetPort().Index + port.GetDataCount()))
            {
                yield return (m_Builder.PortToNodeId[edge.InputPortIndex],
                    new InputDataPort { Port = { Index = edge.InputPortIndex } });
            }
        }
        protected IEnumerable<(NodeId id, IOutputDataPort port)> GetConnectedPort(IInputDataPort port)
        {
            foreach (var edge in m_Builder.m_EdgeTable
                     .Where(e => e.InputPortIndex >= port.GetPort().Index && e.InputPortIndex < port.GetPort().Index + port.GetDataCount()))
            {
                yield return (m_Builder.PortToNodeId[edge.OutputPortIndex],
                    new OutputDataPort { Port = { Index = edge.OutputPortIndex } });
            }
        }

        protected IUnitPort GetUnitPort(PortMapper oldMapping, IPort port)
        {
            var p = oldMapping.AllPorts.FirstOrDefault(x => Equals(x.Value.Port, port));
            return p.Key;
        }

        protected void Replace<T>(NodeId oldNodeId, INode oldNode, T newNode,
            PortMapper newMapping, Dictionary<IPort, IPort> portRemapping) where T : struct, INode
        {
            m_Builder.RemoveNode(oldNodeId, oldNode);
            m_Builder.AddNodeInternal(m_Builder.GetNextNodeId(), newNode, newMapping);
            RemapEdges(portRemapping);
        }

        protected void RemapEdges(Dictionary<IPort, IPort> portRemapping)
        {
            foreach (var remap in portRemapping)
            {
                var oldPortIndex = remap.Key.GetPort().Index;
                var newPortIndex = remap.Value.GetPort().Index;

                for (var i = 0; i < m_Builder.m_EdgeTable.Count; i++)
                {
                    var edge = m_Builder.m_EdgeTable[i];

                    if (edge.InputPortIndex == oldPortIndex)
                        edge.InputPortIndex = newPortIndex;
                    else if (edge.OutputPortIndex == oldPortIndex)
                        edge.OutputPortIndex = newPortIndex;
                    m_Builder.m_EdgeTable[i] = edge;
                }
            }
        }

        protected ILookup<uint, uint> GetOutputPortToConnectedInputPortsLookup()
        {
            if (m_OutputPortToConnectedInputPorts == null)
                m_OutputPortToConnectedInputPorts =
                    m_Builder.m_EdgeTable.ToLookup(e => e.OutputPortIndex, e => e.InputPortIndex);
            return m_OutputPortToConnectedInputPorts;
        }
    }
}
