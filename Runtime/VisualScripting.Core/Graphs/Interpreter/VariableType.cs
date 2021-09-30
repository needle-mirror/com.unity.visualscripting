using System;

namespace Unity.VisualScripting.Interpreter
{
    [Serializable, Flags]
    public enum VariableType : byte
    {
        ObjectReference = 1 << 0,
        Variable = 1 << 1,
        Input = 1 << 2,
        Output = 1 << 3,
        SmartObject = 1 << 4
    }
}
