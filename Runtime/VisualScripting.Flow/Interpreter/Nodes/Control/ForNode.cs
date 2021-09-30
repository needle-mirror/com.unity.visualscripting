namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(For))]
    public struct ForNode : IStatefulNode<ForNode.State>, IUpdatableNode
    {
        struct State : INodeState
        {
            public int LoopId;
            public int CurrentIndex;
            public int FirstIndex;
            public int LastIndex;
            public bool Ascending => LastIndex >= CurrentIndex;
        }
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public OutputTriggerPort Body;
        public InputDataPort FirstIndex;
        public InputDataPort LastIndex;
        public InputDataPort Step;
        public OutputDataPort CurrentIndex;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetState<ForNode, State>(this);
            state.LoopId = ctx.StartLoop();
            state.FirstIndex = ctx.ReadInt(FirstIndex);
            state.LastIndex = ctx.ReadInt(LastIndex);
            state.CurrentIndex = state.FirstIndex;
            ctx.Write(CurrentIndex, state.CurrentIndex);

            return MoveNext(ctx, ref state);
        }

        private bool CanMoveNext<TCtx>(ref State state, in TCtx ctx) where TCtx : IGraphInstance => state.LoopId == ctx.CurrentLoopId && state.Ascending
        ? (state.CurrentIndex < state.LastIndex)
        : (state.CurrentIndex > state.LastIndex);

        private Execution MoveNext<TCtx>(TCtx ctx, ref State state) where TCtx : IGraphInstance
        {
            if (state.LoopId == ctx.CurrentLoopId) // loop not broken
            {
                if (CanMoveNext(ref state, in ctx))
                {
                    ctx.Write(CurrentIndex, state.CurrentIndex);
                    var step = ctx.ReadInt(Step);
                    state.CurrentIndex += step;
                    ctx.Trigger(Body);
                    return Execution.Yield;
                }

                ctx.EndLoop(state.LoopId);
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            return MoveNext(ctx, ref ctx.GetState<ForNode, State>(this));
        }

        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
        }
    }
}
