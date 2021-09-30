using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    public class RuntimeGraphAsset : ScriptableObject
    {
        public GraphDefinition GraphDefinition;
        [SerializeField] public uint Hash;
        public RuntimeGraphDebugData DebugData { get; set; }
    }

    public class RuntimeGraphDebugData
    {
        internal struct NodeAnnotations
        {
            private Dictionary<NodeId, List<NodeAnnotation>> _annotations;

            internal void AddAnnotation(NodeId nodeId, Color color, string label)
            {
                if (_annotations == null)
                    _annotations = new Dictionary<NodeId, List<NodeAnnotation>>();
                if (!_annotations.TryGetValue(nodeId, out var l))
                    _annotations.Add(nodeId, l = new List<NodeAnnotation>());
                l.Add(new NodeAnnotation { Color = color, Label = label });
            }

            internal bool TryGetAnnotations(NodeId nodeId, out List<NodeAnnotation> annotations)
            {
                annotations = null;
                return _annotations != null && _annotations.TryGetValue(nodeId, out annotations);
            }

            public void RemapNodeIds(Dictionary<NodeId, NodeId> nodeIdRemapping)
            {
                if (_annotations == null)
                    return;
                _annotations = _annotations
                    .Select(p =>
                        nodeIdRemapping.TryGetValue(p.Key, out var newId) ? (newId, p.Value) : (p.Key, p.Value))
                    .ToDictionary(x => x.Item1, x => x.Value);
            }

            public void Remove(NodeId oldNodeId)
            {
                _annotations?.Remove(oldNodeId);
            }
        }
        internal struct NodeAnnotation
        {
            public Color Color;
            public string Label;
        }

        internal NodeAnnotations Annotations;

        private Dictionary<NodeId, Guid> m_NodeToGuids;
        private Dictionary<uint, IGraphItem> m_PortIndexToPortUnit;

        internal RuntimeGraphDebugData(Dictionary<NodeId, Guid> nodeToGuids,
            Dictionary<uint, IGraphItem> portIndexToPortUnit, NodeAnnotations annotations)
        {
            m_NodeToGuids = nodeToGuids;
            m_PortIndexToPortUnit = portIndexToPortUnit;
            Annotations = annotations;
        }
        public RuntimeGraphDebugData(Dictionary<NodeId, Guid> nodeToGuids, Dictionary<uint, IGraphItem> portIndexToPortUnit)
            : this(nodeToGuids, portIndexToPortUnit, default)
        {
        }

        public Guid GetNodeGuid(NodeId nodeId)
        {
            return m_NodeToGuids != null && m_NodeToGuids.TryGetValue(nodeId, out var guid) ? guid : default;
        }
        public IEnumerable<NodeId> GetNodeIds(Guid guid)
        {
            return m_NodeToGuids == null ? Enumerable.Empty<NodeId>() : m_NodeToGuids.Where(x => x.Value == guid).Select(x => x.Key);
        }

        public bool GetNodeAndPortFromRuntimePort(Port stepPort, out IGraphItem port)
        {
            port = default;
            return m_PortIndexToPortUnit != null && m_PortIndexToPortUnit.TryGetValue(stepPort.Index, out port);
        }
    }
}
