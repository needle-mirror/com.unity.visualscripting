using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SwitchOnEnum))]
    public struct SwitchOnEnumNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerMultiPort Branches;
        public InputDataPort Selector;
        // should have int values connected (each enum value casted to int) just in case the enum has custom int values (eg. enum X { A = 32, B = 54, ... })
        public InputDataMultiPort EnumValues;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            int selectorValue = ctx.ReadValue(Selector).EnumValue;
            for (uint i = 0; i < EnumValues.DataCount; i++)
            {
                if (selectorValue == ctx.ReadInt(EnumValues.SelectPort(i)))
                {
                    ctx.Trigger(Branches.SelectPort(i));
                    break;
                }
            }

            return Execution.Done;
        }
    }
}
