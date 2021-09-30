namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Break))]
    public struct BreakNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ctx.BreakCurrentLoop();
            return Execution.Done;
        }
    }
}
