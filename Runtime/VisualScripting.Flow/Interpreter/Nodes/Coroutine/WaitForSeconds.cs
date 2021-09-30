using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(WaitForSecondsUnit))]
    public struct WaitForSeconds : ICoroutineStatefulNode<WaitForSeconds.State>, IUpdatableNode
    {
        public struct State : INodePerCoroutineState
        {
            public float Remaining;
            public bool Unscaled;
        }

        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public InputDataPort Seconds;
        public InputDataPort UnscaledTime;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetCoroutineState<WaitForSeconds, State>(in this);
            state.Remaining = ctx.ReadFloat(Seconds);
            state.Unscaled = ctx.ReadBool(UnscaledTime);
            return Process(ref ctx, state);
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetCoroutineState<WaitForSeconds, State>(in this);
            state.Remaining -= state.Unscaled ? ctx.TimeData.UnscaledDeltaTime : ctx.TimeData.DeltaTime;
            return Process(ref ctx, state);
        }

        private Execution Process<TCtx>(ref TCtx ctx, in State state) where TCtx : IGraphInstance
        {
            if (state.Remaining <= 0f)
            {
                ctx.Trigger(Exit);
                return Execution.Done;
            }

            return Execution.Running;
        }
    }

    [NodeDescription(typeof(Cooldown))]
    public struct CooldownNode : IStatefulNode<CooldownNode.State>, IUpdatableNode
    {
        public struct State : INodeState
        {
            public float Remaining;
            public float Duration;
            public bool Unscaled;
            public bool IsReady => Remaining <= 0;
        }

        public InputTriggerPort Enter;
        public InputTriggerPort Reset;
        public OutputTriggerPort ExitReady;
        public OutputTriggerPort ExitNotReady;
        public OutputTriggerPort Tick;
        public OutputTriggerPort BecameReady;
        public InputDataPort Duration;
        public InputDataPort UnscaledTime;
        public OutputDataPort RemainingSeconds;
        public OutputDataPort RemainingRatio;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState<CooldownNode, State>(in this);

            if (port == Reset)
            {
                state = DoReset(ctx);
            }
            else // Enter
            {
                if (state.IsReady)
                {
                    bool wasReady = state.IsReady;
                    // will start the timer
                    state = DoReset(ctx);
                    if (wasReady)
                        ctx.Trigger(ExitReady);
                }
                else
                {
                    ctx.Trigger(ExitNotReady);
                }
            }

            WriteRemainingData(ctx, state);
            // no need to update if duration is 0
            return state.Duration > 0 ? Execution.Running : Execution.Done;
        }

        private void WriteRemainingData<TCtx>(TCtx ctx, State state) where TCtx : IGraphInstance
        {
            ctx.Write(RemainingSeconds, state.Remaining);
            ctx.Write(RemainingRatio, Mathf.Clamp01(state.Remaining / state.Duration));
        }

        private State DoReset<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            State state;
            state.Remaining = state.Duration = ctx.ReadFloat(Duration);
            state.Unscaled = ctx.ReadBool(UnscaledTime);
            return state;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetState<CooldownNode, State>(in this);
            bool wasReady = state.IsReady;
            state.Remaining = Mathf.Max(0,
                state.Remaining - (state.Unscaled ? ctx.TimeData.UnscaledDeltaTime : ctx.TimeData.DeltaTime));
            WriteRemainingData(ctx, state);
            bool nowReady = state.IsReady;
            ctx.Trigger(Tick);
            if (!wasReady && nowReady)
                ctx.Trigger(BecameReady);
            // if now ready, this was the last update
            return nowReady ? Execution.Done : Execution.Running;
        }

        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
        }
    }
}
