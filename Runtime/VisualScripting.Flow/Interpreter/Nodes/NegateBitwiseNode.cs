using System;

namespace Unity.VisualScripting.Interpreter
{
    public struct NegateBitwiseNode : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadValue(Input);
            Value result;
            switch (input.Type)
            {
                case ValueType.Bool:
                    result = !input.Bool;
                    break;
                case ValueType.Int:
                    result = ~input.Int;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ctx.Write(Output, result);
        }
    }
}
