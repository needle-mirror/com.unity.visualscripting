using System;
using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SelectOnString))]
    public struct SelectOnStringNode : IDataNode
    {
        public InputDataPort Selector;
        public InputDataPort Default;
        public InputDataMultiPort OptionValues;
        public InputDataMultiPort OptionPorts;
        public OutputDataPort Selection;
        public bool IgnoreCase;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var selector = ctx.ReadObject<string>(Selector);
            for (uint i = 0; i < OptionValues.DataCount; i++)
            {
                if (string.Equals(selector, ctx.ReadObject<string>(OptionValues.SelectPort(i)),
                    IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    ctx.Write(Selection, ctx.ReadValue(OptionPorts.SelectPort(i)));
                    return;
                }
            }
            ctx.Write(Selection, ctx.ReadValue(Default));
        }
    }
}
