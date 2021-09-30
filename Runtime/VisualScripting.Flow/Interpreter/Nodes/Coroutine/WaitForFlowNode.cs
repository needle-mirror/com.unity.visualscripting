namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(WaitForFlow))]
    public struct WaitForFlowNode : IStatefulNode<WaitForFlowNode.State>
    {
        public struct State : INodeState
        {
            public uint Activated;
        }
        public InputTriggerMultiPort Inputs;
        public InputTriggerPort Reset;
        public OutputTriggerPort Exit;
        public bool ResetOnExit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState<WaitForFlowNode, State>(in this);
            if (port == Reset)
            {
                state.Activated = 0;
                return Execution.Done;
            }
            uint index = port.Port.Index - Inputs.Port.Index;
            state.Activated |= 1u << (int)index;
            uint allTriggered = Inputs.DataCount == 32
                // 1111...1
                ? ~0u
                // for a count of 2: 1 << 2 = 100, 100 - 1 = 011
                : (1u << Inputs.DataCount) - 1;
            if (state.Activated == allTriggered)
            {
                if (ResetOnExit)
                    state.Activated = 0;
                ctx.Trigger(Exit);
            }

            return Execution.Done;
        }

        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
        }
    }
}
