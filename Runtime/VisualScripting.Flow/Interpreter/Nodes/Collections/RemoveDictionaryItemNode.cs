using System.Collections;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(RemoveDictionaryItem))]
    public struct RemoveDictionaryItemNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort DictionaryInput;
        public OutputDataPort DictionaryOutput;
        public InputDataPort Key;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var dict = ctx.ReadObject<IDictionary>(DictionaryInput);
            dict.Remove(ctx.ReadObject<object>(Key));
            ctx.Write(DictionaryOutput, Value.FromObject(dict));
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
