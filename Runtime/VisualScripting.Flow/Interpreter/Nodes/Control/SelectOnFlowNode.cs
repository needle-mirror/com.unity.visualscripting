using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(SelectOnFlow))]
    public struct SelectOnFlowNode : IFlowNode
    {
        public OutputTriggerPort Exit;
        public OutputDataPort Selection;
        public InputTriggerMultiPort Inputs;
        // should have int values connected (each enum value casted to int) just in case the enum has custom int values (eg. enum X { A = 32, B = 54, ... })
        public InputDataMultiPort Values;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var idx = Inputs.GetSubPortIndex(port);
            ctx.Write(Selection, ctx.ReadValue(Values.SelectPort(idx)));
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
