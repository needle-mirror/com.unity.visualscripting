using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SelectOnInteger))]
    public struct SelectOnIntegerNode : IDataNode
    {
        public InputDataPort Selector;
        public InputDataPort Default;
        public InputDataMultiPort OptionValues;
        public InputDataMultiPort OptionPorts;
        public OutputDataPort Selection;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var selector = ctx.ReadInt(Selector);
            for (uint i = 0; i < OptionValues.DataCount; i++)
            {
                if (selector == ctx.ReadInt(OptionValues.SelectPort(i)))
                {
                    ctx.Write(Selection, ctx.ReadValue(OptionPorts.SelectPort(i)));
                    return;
                }
            }
            ctx.Write(Selection, ctx.ReadValue(Default));
        }
    }
}
