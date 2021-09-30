using System.Collections;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SetDictionaryItem))]
    public struct SetDictionaryItemNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort Dictionary;
        public InputDataPort Key;
        public InputDataPort Value;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var dict = ctx.ReadObject<IDictionary>(Dictionary);
            var key = ctx.ReadObject<object>(Key);
            var value = ctx.ReadObject<object>(Value);
            dict[key] = value;
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
