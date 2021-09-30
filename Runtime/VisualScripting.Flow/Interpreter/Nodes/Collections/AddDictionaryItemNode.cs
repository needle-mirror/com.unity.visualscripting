using System.Collections;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(AddDictionaryItem))]
    public struct AddDictionaryItemNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public InputDataPort DictionaryInput;
        public InputDataPort Key;
        public InputDataPort Value;
        public OutputDataPort DictionaryOutput;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var d = ctx.ReadObject<IDictionary>(DictionaryInput);
            var key = ctx.ReadObject<object>(Key);
            var value = ctx.ReadObject<object>(Value);
            d.Add(key, value);
            ctx.Write(DictionaryOutput, Interpreter.Value.FromObject(d));
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
