using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// pre-computes constant operations. 1 + (2*3) becomes 7. Start from constant nodes, recursively visit connected
    /// data nodes to get a topological sort of the graph. any data node connected to a flow node will trigger a fold
    /// and that data node's outputs will be remapped to the newly created constants.
    /// the actual computation is done using a stripped down IGraphInstance impl
    /// </summary>
    class ConstantFoldingTransform : GraphTransform
    {
        internal const string k_InternalVSToggleConstantfolding = "internal:Visual Scripting/Toggle ConstantFolding";
        internal const string k_InternalVSDebugConstantfolding = "internal:Visual Scripting/Debug ConstantFolding";



        internal static class FoldableData
        {
            // TODO: allow user overrides
            // users can override the foldability of runtime node types - eg. an asset store creator forgot to flag a node as foldable
            // private static Dictionary<Type, bool> UnitTypeToFoldable;

            // users can override the foldability of a specific method/getter/... - eg. some lib offers a function that is pure, and hence foldable
            // this should be used to fold reflected member nodes AND to flag or not code generated api nodes
            private static Dictionary<int, bool> MemberToFoldable = new Dictionary<int, bool>
            {
                { new Member(typeof(Vector3), typeof(Vector3).GetProperty(nameof(Vector3.down))).GetHashCode(), true }
            };

            public static bool IsFoldable(Member reflectedMember)
            {
                if (MemberToFoldable.TryGetValue(reflectedMember.GetHashCode(), out var predicate))
                    return predicate;
                return false;
            }
            public static bool IsFoldable(GraphBuilder builder, INode u)
            {
                if (u is IFoldableNode)
                    return true;
                // UnitTypeToFoldable.TryGetValue(u.GetType(), out var foldableType) && foldableType ||
                if (u is IReflectedMemberNode memberUnit)
                {
                    Member reflectedMember = builder.GetReflectedMember(memberUnit.ReflectedMemberIndex);
                    if (MemberToFoldable.TryGetValue(reflectedMember.GetHashCode(), out var predicate))
                        return predicate;
                }

                return false;
            }
        }

        [MenuItem(k_InternalVSToggleConstantfolding)]
        static void Toggle() => EditorPrefs.SetBool(k_InternalVSToggleConstantfolding, !EditorPrefs.GetBool(k_InternalVSToggleConstantfolding));
        [MenuItem(k_InternalVSDebugConstantfolding)]
        static void Debug() => EditorPrefs.SetBool(k_InternalVSDebugConstantfolding, !EditorPrefs.GetBool(k_InternalVSDebugConstantfolding));

        readonly Dictionary<uint, Value> m_Values;
        readonly Dictionary<NodeId, (bool, RangeInt)> m_NodeIsFoldable;
        readonly List<NodeId> m_TopologicalSort;

        bool CannotBeFoldedAtAll(INode n) => !FoldableData.IsFoldable(m_Builder, n);

        private int currentLevel;
        private HashSet<NodeId> m_NodesToFold;
        private HashSet<NodeId> m_NodesToDelete;

        public ConstantFoldingTransform()
        {
            m_Values = new Dictionary<uint, Value>();
            m_NodeIsFoldable = new Dictionary<NodeId, (bool, RangeInt)>();
            m_TopologicalSort = new List<NodeId>();
            m_NodesToFold = new HashSet<NodeId>();
            m_NodesToDelete = new HashSet<NodeId>();
        }

        protected override void DoRun()
        {
            var startNodes = m_Builder.NodeTable
                .Where(n => CannotBeFoldedAtAll(n.Value.node))
                .Select(n => n.Key);

            // visit each node, which will recurse left as needed
            // this will fill the m_NodesToFold and m_NodesToDelete lists, and also evaluate all nodes that can be folded
            foreach (var nodeId in startNodes)
            {
                m_Builder.m_NodeAnnotations.AddAnnotation(nodeId, new Color(0.73f, 0.88f, 1.00f), "Start");
                Visit(nodeId);
            }

            if (EditorPrefs.GetBool(k_InternalVSDebugConstantfolding))
                return;

            // insert the new nodes
            foreach (var nodeId in m_NodesToFold)
            {
                DoFold(nodeId);
            }
            // delete all redundant nodes
            foreach (var nodeId in m_NodesToDelete)
            {
                m_Builder.RemoveNode(nodeId, m_Builder.NodeTable[nodeId].node);
            }
        }

        protected override void DoVisit(NodeId nodeId, INode n, PortMapper nodeMapper)
        {
            int curIndex = m_TopologicalSort.Count;
            m_TopologicalSort.Add(nodeId);
            var cannotBeFoldedAtAll = CannotBeFoldedAtAll(n);

            var canBeFolded = CanBeFolded();
            m_Builder.m_NodeAnnotations.AddAnnotation(nodeId, canBeFolded ? new Color(0.73f, 1.00f, 0.79f) : new Color(0.96f, 0.45f, 0.58f), $"{(canBeFolded ? "" : "Not ")}foldable");

            m_NodeIsFoldable.Add(nodeId, (canBeFolded, new RangeInt(curIndex, m_TopologicalSort.Count - curIndex)));

            if (canBeFolded)
            {
                switch (n)
                {
                    case IConstantNode constantNode:
                        {
                            var ctx = new DataGraphInstance(m_Values, GetOutputPortToConnectedInputPortsLookup(), m_Builder);
                            constantNode.Execute(ctx);
                            break;
                        }
                    case IDataNode dataNode:
                        {
                            var ctx = new DataGraphInstance(m_Values, GetOutputPortToConnectedInputPortsLookup(), m_Builder);
                            dataNode.Execute(ctx);
                            break;
                        }
                }

            }
            if (!canBeFolded)
            {
                foreach (var (_, port) in FlowGraphTranslator.GetNodeInputPorts(n))
                {
                    foreach (var connected in GetConnectedPort(port))
                    {
                        if (m_NodeIsFoldable.TryGetValue(connected.id, out var foldableAndIndex) && foldableAndIndex.Item1 &&
                            !(m_Builder.NodeTable[connected.id].node is IConstantNode))
                        {
                            if (m_NodesToFold.Add(connected.id))
                                m_Builder.m_NodeAnnotations.AddAnnotation(connected.id, new Color(1.00f, 1.00f, 0.73f), $"DO FOLD");
                            for (int i = foldableAndIndex.Item2.start; i < foldableAndIndex.Item2.end; i++)
                            {
                                if (m_NodesToDelete.Add(m_TopologicalSort[i]))
                                    m_Builder.m_NodeAnnotations.AddAnnotation(m_TopologicalSort[i], new Color(0.82f, 0.72f, 0.78f), $"To delete");
                            }
                        }
                    }
                }
            }

            bool CanBeFolded()
            {
                bool allFoldable = true;
                foreach (var (_, port) in FlowGraphTranslator.GetNodeInputPorts(n))
                {
                    foreach (var connected in GetConnectedPort(port))
                    {
                        Visit(connected.id);
                        if (m_NodeIsFoldable[connected.id].Item1)
                            continue;
                        allFoldable = false;
                    }
                }

                return allFoldable && !cannotBeFoldedAtAll;
            }
        }

        private void DoFold(NodeId nodeId)
        {
            PortMapper nodeMapper = m_Builder.NodeTable[nodeId].mapper;
            INode node = m_Builder.NodeTable[nodeId].node;
            Dictionary<IPort, IPort> portRemapping = new Dictionary<IPort, IPort>();

            foreach (var (_, port) in FlowGraphTranslator.GetNodeOutputPorts(node))
            {
                if (port == null)
                    continue;
                for (uint i = 0; i < port.GetDataCount(); i++)
                {
                    var p = port.SelectOutputDataPort(i);
                    if (p.Port.Index == 0)
                        continue;
                    var computedConstantValue = m_Values[p.Port.Index].Box();

                    var literalOutputPort = GetUnitPort(nodeMapper, p);
                    FlowGraphTranslator.TranslateConstant(m_Builder, out var computedConstant,
                        out var computedMapping, computedConstantValue.GetType(), computedConstantValue,
                        literalOutputPort,
                        out var constantOutputPort);

                    m_Builder.AddNodeInternal(m_Builder.GetNextNodeId(), computedConstant, computedMapping, m_Builder.GetSourceNodeModel(nodeId));
                    portRemapping.Add(p, constantOutputPort);
                }
            }

            RemapEdges(portRemapping);
        }
    }
}
