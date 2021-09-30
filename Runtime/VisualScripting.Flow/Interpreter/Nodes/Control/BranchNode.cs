using UnityEngine.Scripting.APIUpdating;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(If))]
    [MovedFrom(false, sourceClassName: "BranchNode")]
    public struct BranchNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort Condition;
        public OutputTriggerPort IfTrue;
        public OutputTriggerPort IfFalse;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ctx.Trigger(ctx.ReadBool(Condition) ? IfTrue : IfFalse);
            return Execution.Done;
        }
    }
}
