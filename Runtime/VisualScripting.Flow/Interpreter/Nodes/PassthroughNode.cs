using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting.Interpreter
{
    public struct PassthroughNode : IDataNode, IFoldableNode
    {
        public static PassthroughNode Create(int inputOutputCount) => new PassthroughNode
        {
            Input = { DataCount = inputOutputCount },
            Output = { DataCount = inputOutputCount },
        };
        public InputDataMultiPort Input;
        public OutputDataMultiPort Output;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            for (uint i = 0; i < Input.DataCount; i++)
            {
                ctx.Write(Output.SelectPort(i), ctx.ReadValue(Input.SelectPort(i)));
            }
        }
    }
}
