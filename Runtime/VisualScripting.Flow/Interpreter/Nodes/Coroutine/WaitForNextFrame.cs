namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(WaitForNextFrameUnit))]
    public struct WaitForNextFrameNode : IUpdatableNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            return Execution.Running;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
