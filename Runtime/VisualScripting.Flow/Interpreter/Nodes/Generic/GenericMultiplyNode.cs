using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(GenericMultiply))]
    public struct GenericMultiplyNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public OutputDataPort Product;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Product, Value.FromObject(OperatorUtility.Multiply(
                ctx.ReadValue(A).Box(),
                ctx.ReadValue(B).Box())));
        }
    }
}
