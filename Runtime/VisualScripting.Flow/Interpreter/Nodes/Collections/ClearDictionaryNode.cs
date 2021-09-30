using System.Collections;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(ClearDictionary))]
    public struct ClearDictionaryNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort DictionaryInput;
        public OutputDataPort DictionaryOutput;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var dict = ctx.ReadObject<IDictionary>(DictionaryInput);
            dict.Clear();
            ctx.Write(DictionaryOutput, Value.FromObject(dict));
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
