namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(ToggleValue))]
    public struct ToggleValueNode : IStatefulNode<ToggleValueNode.State>
    {
        public struct State : INodeState
        {
            public bool IsOn;
        }

        public bool StartOn;
        public InputTriggerPort TurnOn;
        public InputTriggerPort TurnOff;
        public InputTriggerPort Toggle;

        public OutputTriggerPort TurnedOn;
        public OutputTriggerPort TurnedOff;
        public OutputDataPort IsOn;


        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetState<ToggleValueNode, State>(this);
            state.IsOn = StartOn;
            ctx.Write(IsOn, StartOn);
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetState<ToggleValueNode, State>(this);
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

            return Execution.Done;
        }
    }
}
