using System.Collections;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(MergeDictionaries))]
    public struct MergeDictionariesNode : IDataNode
    {
        [PortDescription("multiInput")]
        public InputDataMultiPort Elements;

        public OutputDataPort Dictionary;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var result = new AotDictionary();

            for (uint i = 0; i < Elements.DataCount; i++)
            {
                var dict = ctx.ReadObject<IDictionary>(Elements.SelectPort(i));
                var enumerator = dict.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if (!result.Contains(enumerator.Key))
                    {
                        result.Add(enumerator.Key, enumerator.Value);
                    }
                }
            }

            ctx.Write(Dictionary, Value.FromObject(result));
        }
    }
}
