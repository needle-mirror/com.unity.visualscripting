namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(While))]
    public struct WhileNode : IUpdatableNode
    {
        public InputTriggerPort Enter;
        public InputDataPort Condition;

        public OutputTriggerPort Exit;
        public OutputTriggerPort Body;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            return Update(ctx);
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            if (ctx.ReadBool(Condition))
            {
                ctx.Trigger(Body);
                return Execution.Yield;
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
