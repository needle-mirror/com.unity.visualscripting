using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    public struct NegateNumericNode : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadValue(Input);
            Value result;
            switch (input.Type)
            {
                case ValueType.Int:
                    result = -input.Int;
                    break;
                case ValueType.Float:
                    result = -input.Float;
                    break;
                case ValueType.Float2:
                    result = -input.Float2;
                    break;
                case ValueType.Float3:
                    result = -input.Float3;
                    break;
                case ValueType.Float4:
                    result = -input.Float4;
                    break;
                case ValueType.Quaternion:
                    result = Quaternion.Inverse(input.Quaternion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ctx.Write(Output, result);
        }
    }
}
