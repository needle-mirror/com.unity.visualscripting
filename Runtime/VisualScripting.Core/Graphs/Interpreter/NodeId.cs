using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// Represents an index in the graphInstance node list. base 1, so 0 means the node id is invalid or not initialized
    /// </summary>
    [Serializable]
    public struct NodeId : IEquatable<NodeId>
    {
        [SerializeField]
        uint m_NodeIndex;

        public static NodeId Null => default;

        public NodeId(uint index)
        {
            m_NodeIndex = index + 1;
        }

        public uint GetIndex()
        {
            return m_NodeIndex - 1;
        }

        public bool IsValid()
        {
            return m_NodeIndex > 0 && m_NodeIndex < 0x7FFFFFFF;
        }

        public override string ToString()
        {
            return $"${m_NodeIndex}";
        }

        public bool Equals(NodeId other)
        {
            return m_NodeIndex == other.m_NodeIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is NodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)m_NodeIndex;
        }

        public static bool operator ==(NodeId left, NodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NodeId left, NodeId right)
        {
            return !left.Equals(right);
        }
    }
}
