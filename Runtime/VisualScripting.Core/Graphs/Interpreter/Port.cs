using System;

namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// Basic Port with an id. could be renamed PortId. this index is unique on a graph level and is used to
    /// index the graph definition's port info table. Wrapped by strongly typed ports (InputDataPort, ...)
    /// </summary>
    [Serializable]
    public struct Port : IEquatable<Port>
    {
        public uint Index;

        public static bool operator ==(Port lhs, Port rhs)
        {
            return lhs.Index == rhs.Index;
        }

        public static bool operator !=(Port lhs, Port rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Port other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is Port other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Index;
        }
    }
}
