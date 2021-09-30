using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(GenericSum))]
    public struct GenericSumNode : IDataNode, IFoldableNode
    {
        public InputDataMultiPort Inputs;
        public OutputDataPort Sum;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var result = ctx.ReadValue(Inputs.SelectPort(0)).Box();
            for (uint i = 1; i < Inputs.DataCount; i++)
                result = OperatorUtility.Add(result, ctx.ReadValue(Inputs.SelectPort(i)).Box());
            ctx.Write(Sum, Value.FromObject(result));
        }
    }
}
