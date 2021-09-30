using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SelectOnEnum))]
    public struct SelectOnEnumNode : IDataNode
    {
        public InputDataPort Selector;
        public OutputDataPort Selection;
        // should have int values connected (each enum value casted to int) just in case the enum has custom int values (eg. enum X { A = 32, B = 54, ... })
        public InputDataMultiPort EnumValues;
        public InputDataMultiPort Values;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            int selectorValue = ctx.ReadValue(Selector).EnumValue;
            for (uint i = 0; i < EnumValues.DataCount; i++)
            {
                if (selectorValue == ctx.ReadInt(EnumValues.SelectPort(i)))
                {
                    ctx.Write(Selection, ctx.ReadValue(Values.SelectPort(i)));
                    break;
                }
            }
        }
    }
}
