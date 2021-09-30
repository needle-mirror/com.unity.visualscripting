namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(ToggleFlow))]
    public struct ToggleFlowNode : IStatefulNode<ToggleFlowNode.State>
    {
        public struct State : INodeState
        {
            public bool IsOn;
        }
        public bool StartOn;

        public InputTriggerPort Enter;
        public InputTriggerPort TurnOn;
        public InputTriggerPort TurnOff;
        public InputTriggerPort Toggle;

        public OutputTriggerPort ExitOn;
        public OutputTriggerPort ExitOff;
        public OutputTriggerPort TurnedOn;
        public OutputTriggerPort TurnedOff;
        public OutputDataPort IsOn;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetState<ToggleFlowNode, State>(this);
            if (port == TurnOn)
            {
                if (!state.IsOn)
                {
                    state.IsOn = true;
                    ctx.Write(IsOn, state.IsOn);
                    ctx.Trigger(TurnedOn);
                }
            }
            else if (port == TurnOff)
            {
                if (state.IsOn)
                {
                    state.IsOn = false;
                    ctx.Write(IsOn, state.IsOn);
                    ctx.Trigger(TurnedOff);
                }
            }
            else if (port == Toggle)
            {
                state.IsOn = !state.IsOn;
                ctx.Write(IsOn, state.IsOn);
                ctx.Trigger(state.IsOn ? TurnedOn : TurnedOff);
            }
            else
            {
                ctx.Trigger(state.IsOn ? ExitOn : ExitOff);
            }

            return Execution.Done;
        }

        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetState<ToggleFlowNode, State>(this);
            state.IsOn = StartOn;
            ctx.Write(IsOn, StartOn);
        }
    }
}
