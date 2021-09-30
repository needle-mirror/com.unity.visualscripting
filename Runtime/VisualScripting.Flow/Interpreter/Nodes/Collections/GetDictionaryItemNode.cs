using System.Collections;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(GetDictionaryItem))]
    public struct GetDictionaryItemNode : IDataNode
    {
        public InputDataPort Dictionary;
        public InputDataPort Key;
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var dict = ctx.ReadObject<IDictionary>(Dictionary);
            var key = ctx.ReadObject<object>(Key);
            ctx.Write(Value, Interpreter.Value.FromObject(dict[key]));
        }
    }
}
