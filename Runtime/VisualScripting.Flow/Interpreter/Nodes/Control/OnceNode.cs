using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Once))]
    public struct OnceNode : IStatefulNode<OnceNode.State>
    {
        public struct State : INodeState
        {
            public bool Done;
        }

        public InputTriggerPort Enter;
        public InputTriggerPort Reset;
        public OutputTriggerPort Once;
        public OutputTriggerPort After;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState<OnceNode, State>(in this);
            if (port == Reset)
            {
                state.Done = false;
                return Execution.Done;
            }

            if (state.Done)
                ctx.Trigger(After);
            else
            {
                state.Done = true;
                ctx.Trigger(Once);
            }

            return Execution.Done;
        }

        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
        }
    }
}
