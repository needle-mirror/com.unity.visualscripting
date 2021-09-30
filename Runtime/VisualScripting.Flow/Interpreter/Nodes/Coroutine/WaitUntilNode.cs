namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(WaitUntilUnit))]
    public struct WaitUntilNode : IUpdatableNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public InputDataPort Condition;
        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            return Update(ctx);
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            if (ctx.ReadBool(Condition))
            {
                ctx.Trigger(Exit);
                return Execution.Done;
            }

            return Execution.Running;
        }
    }
}
