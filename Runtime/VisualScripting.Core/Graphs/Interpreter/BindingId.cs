using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct BindingId : IEquatable<BindingId>
    {
        [FieldOffset(0), SerializeField]
        private ulong Half1;
        [FieldOffset(8), SerializeField]
        private ulong Half2;

        public bool IsNull => this.Equals(default);

        public static BindingId ToBindingId(string s) => BindingId.From((ulong)s.GetHashCode(), 0);

        public static BindingId From(ulong p1, ulong p2) => new BindingId
        {
            Half1 = p1,
            Half2 = p2,
        };

        public bool Equals(BindingId other)
        {
            return Half1 == other.Half1 && Half2 == other.Half2;
        }

        public override bool Equals(object obj)
        {
            return obj is BindingId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Half1.GetHashCode() * 397) ^ Half2.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{Half1:X8}{Half2:X8} / {(long)Half1} {(long)Half2}";
        }
    }
}
