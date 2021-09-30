using System.Collections;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(WaitForEndOfFrameUnit))]
    public struct WaitForEndOfFrameNode : IUpdatableNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            return Execution.YieldUntilEndOfFrame;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
