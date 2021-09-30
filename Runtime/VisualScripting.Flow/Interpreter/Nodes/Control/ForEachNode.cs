using System.Collections;
using System.Runtime.InteropServices;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(ForEach))]
    public struct ForEachNode : IStatefulNode<ForEachNode.State>, IUpdatableNode
    {
        public bool Dictionary;
        struct State : INodeState
        {
            public int LoopId;
            public int CurrentIndex;
            public GCHandle EnumeratorHandle;

            public IEnumerator Enumerator =>
                EnumeratorHandle.IsAllocated ? EnumeratorHandle.Target as IEnumerator : null;
        }
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public OutputTriggerPort Body;
        public InputDataPort Collection;
        public OutputDataPort CurrentIndex;
        public OutputDataPort CurrentKey;
        public OutputDataPort CurrentItem;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref var state = ref ctx.GetState<ForEachNode, State>(this);
            state.LoopId = ctx.StartLoop();
            state.CurrentIndex = 0;
            state.EnumeratorHandle = GCHandle.Alloc(ctx.ReadObject<IEnumerable>(Collection).GetEnumerator());
            WriteCurrentItems(ref ctx, ref state);

            return MoveNext(ctx, ref state);
        }

        private bool CanMoveNext<TCtx>(ref State state, in TCtx ctx) where TCtx : IGraphInstance =>
            state.LoopId == ctx.CurrentLoopId && state.Enumerator?.MoveNext() == true;

        private Execution MoveNext<TCtx>(TCtx ctx, ref State state) where TCtx : IGraphInstance
        {
            if (state.LoopId == ctx.CurrentLoopId) // loop not broken
            {
                if (CanMoveNext(ref state, in ctx))
                {
                    WriteCurrentItems(ref ctx, ref state);
                    state.CurrentIndex++;
                    ctx.Trigger(Body);
                    return Execution.Yield;
                }

                ctx.EndLoop(state.LoopId);
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }

        private void WriteCurrentItems<TCtx>(ref TCtx ctx, ref State state) where TCtx : IGraphInstance
        {
            ctx.Write(CurrentIndex, state.CurrentIndex);
            if (Dictionary)
            {
                IDictionaryEnumerator dictionaryEnumerator = state.Enumerator as IDictionaryEnumerator;
                ctx.Write(CurrentKey, Value.FromObject(dictionaryEnumerator?.Key));
                ctx.Write(CurrentItem, Value.FromObject(dictionaryEnumerator?.Value));
            }
            else
                ctx.Write(CurrentItem, Value.FromObject(state.Enumerator?.Current));
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            return MoveNext(ctx, ref ctx.GetState<ForEachNode, State>(this));
        }

        public void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
        }
    }
}
