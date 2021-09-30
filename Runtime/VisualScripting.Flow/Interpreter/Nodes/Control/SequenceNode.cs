
namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Sequence))]
    public struct SequenceNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerMultiPort Outputs;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            for (int i = 0; i < Outputs.DataCount; i++)
                ctx.Trigger(Outputs.SelectPort((uint)i));
            return Execution.Done;
        }
    }
}
