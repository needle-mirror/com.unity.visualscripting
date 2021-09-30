using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SelectUnit))]
    public struct SelectNode : IDataNode
    {
        public InputDataPort Condition;
        public InputDataPort IfTrue;
        public InputDataPort IfFalse;
        public OutputDataPort Selection;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Selection, ctx.ReadBool(Condition) ? ctx.ReadValue(IfTrue) : ctx.ReadValue(IfFalse));
        }
    }
}
