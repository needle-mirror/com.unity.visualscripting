using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SwitchOnString))]
    public struct SwitchOnStringNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerMultiPort Branches;
        public OutputTriggerPort Default;
        public InputDataPort Selector;
        public InputDataMultiPort ComparedValues;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            string selectorValue = ctx.ReadObject<string>(Selector);
            for (uint i = 0; i < ComparedValues.DataCount; i++)
            {
                if (selectorValue == ctx.ReadObject<string>(ComparedValues.SelectPort(i)))
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
