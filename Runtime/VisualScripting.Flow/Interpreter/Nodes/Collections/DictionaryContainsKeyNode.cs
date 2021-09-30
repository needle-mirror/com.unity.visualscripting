using System.Collections;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(DictionaryContainsKey))]
    public struct DictionaryContainsKeyNode : IDataNode
    {
        public InputDataPort Dictionary;
        public InputDataPort Key;
        public OutputDataPort Contains;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var dict = ctx.ReadObject<IDictionary>(Dictionary);
            var key = ctx.ReadObject<object>(Key);
            ctx.Write(Contains, dict.Contains(key));
        }
    }
}
