using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(GenericDivide))]
    public struct GenericDivideNode : IDataNode, IFoldableNode
    {
        public InputDataPort Dividend;
        public InputDataPort Divisor;
        public OutputDataPort Quotient;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Quotient, Value.FromObject(OperatorUtility.Divide(
                ctx.ReadValue(Dividend).Box(),
                ctx.ReadValue(Divisor).Box())));
        }
    }
}
