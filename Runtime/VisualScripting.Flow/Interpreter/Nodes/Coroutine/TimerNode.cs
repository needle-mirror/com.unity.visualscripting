using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Timer))]
    public struct TimerNode : IStatefulNode<TimerNode.State>, IUpdatableNode
    {
        public InputTriggerPort Start;
        public InputTriggerPort Pause;
        public InputTriggerPort Resume;
        public InputTriggerPort Toggle;

        public InputDataPort Duration;
        public InputDataPort UnscaledTime;

        public OutputTriggerPort Started;
        public OutputTriggerPort Tick;
        public OutputTriggerPort Completed;

        public OutputDataPort ElapsedSeconds;
        public OutputDataPort ElapsedRatio;
        public OutputDataPort RemainingSeconds;
        public OutputDataPort RemainingRatio;

        public struct State : INodeState
        {
            public float Elapsed;
            public float Duration;
            public bool Active;
            public bool Paused;
            public bool Unscaled;
            public bool Done => Elapsed >= Duration;
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState<TimerNode, State>(in this);
            if (port == Start)
            {
                state = DoStart(ctx);
            }
            else if (port == Pause)
            {
                state.Paused = true;
            }
            else if (port == Resume)
            {
                state.Paused = false;
            }
            else // if (port == Toggle)
            {
                if (!state.Active)
                    state = DoStart(ctx);
                else
                    state.Paused = !state.Paused;
            }
            return state.Active ? Execution.Running : Execution.Done;
        }

        private State DoStart<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            State state;
            state.Elapsed = 0;
            state.Active = true;
            state.Paused = false;
            state.Duration = ctx.ReadFloat(Duration);
            state.Unscaled = ctx.ReadBool(UnscaledTime);
            WriteData(ctx, state);
            ctx.Trigger(Started);
            if (!state.Done)
                ctx.Trigger(Tick);
            else
            {
                ctx.Trigger(Completed);
                state.Active = false;
            }

            return state;
        }

        private void WriteData<TCtx>(TCtx ctx, in State state) where TCtx : IGraphInstance
        {
            ctx.Write(ElapsedSeconds, state.Elapsed);
            ctx.Write(ElapsedRatio, Mathf.Clamp01(state.Elapsed / state.Duration));
            ctx.Write(RemainingSeconds, Mathf.Max(0, state.Duration - state.Elapsed));
            ctx.Write(RemainingRatio, Mathf.Clamp01((state.Duration - state.Elapsed) / state.Duration));
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState<TimerNode, State>(in this);
            if (!state.Active)
                return Execution.Done;
            if (state.Paused)
                return Execution.Running;

            state.Elapsed = Mathf.Min(state.Duration, state.Elapsed + (state.Unscaled ? ctx.TimeData.UnscaledDeltaTime : ctx.TimeData.DeltaTime));
            WriteData(ctx, state);
            ctx.Trigger(Tick);
            if (state.Done)
            {
                state.Active = false;
                ctx.Trigger(Completed);
                return Execution.Done;
            }

            return Execution.Running;
        }

        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
        }
    }
}
