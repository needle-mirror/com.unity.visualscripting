using Unity.VisualScripting;
using UnityEditor;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Cache))]
    public struct CacheNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        // this one is pulled. it's also the input mapped to the cache unit input
        // public InputDataPort Input;
        // those ones are just copied to the output. when api nodes can be data nodes or flow nodes and are used as flow nodes
        public InputDataMultiPort CopiedInputs;
        // public OutputDataPort Output;
        public OutputDataMultiPort CopiedOutputs;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            // ctx.Write(Output, ctx.ReadValue(Input));
            for (uint i = 0; i < CopiedInputs.DataCount; i++)
            {
                // TODO add param to readvalue to skip data pulling for ports > 0
                ctx.Write(CopiedOutputs.SelectPort(i), ctx.ReadValue(CopiedInputs.SelectPort(i)));
            }
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
