using Unity.VisualScripting;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(ScalarNormalize))]
    public struct ScalarNormalizeNode : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadFloat(Input);
            // is that Mathf.Sign() ??
            ctx.Write(Output, input == 0 ? 0 : input / Mathf.Abs(input));
        }
    }
}
