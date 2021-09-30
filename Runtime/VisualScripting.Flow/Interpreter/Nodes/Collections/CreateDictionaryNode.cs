namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(CreateDictionary))]
    public struct CreateDictionaryNode : IDataNode
    {
        public OutputDataPort Dictionary;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Dictionary, Value.FromObject(new AotDictionary()));
        }
    }
}
