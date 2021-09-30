using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SwitchOnInteger))]
    public struct SwitchOnIntegerNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerMultiPort Branches;
        public OutputTriggerPort Default;
        public InputDataPort Selector;
        public InputDataMultiPort ComparedValues;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            int selectorValue = ctx.ReadInt(Selector);
            for (uint i = 0; i < ComparedValues.DataCount; i++)
            {
                if (selectorValue == ctx.ReadInt(ComparedValues.SelectPort(i)))
                {
                    ctx.Trigger(Branches.SelectPort(i));
                    return Execution.Done;
                }
            }

            ctx.Trigger(Default);

            return Execution.Done;
        }
    }
}
