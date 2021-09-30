using System;

namespace Unity.VisualScripting.Interpreter
{
    [Serializable]
    public enum ValueType : byte
    {
        Unknown = 0,
        Bool = 1,
        Int = 2,
        Float = 3,
        Float2 = 4,
        Float3 = 5,
        Float4 = 6,
        Quaternion = 7,
        ManagedObject = 8,
        Struct = 9,
        Enum = 10,
        Color = 11,
    }
}
