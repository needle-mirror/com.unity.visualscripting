using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(GenericModulo))]
    public struct GenericModuloNode : IDataNode, IFoldableNode
    {
        public InputDataPort Dividend;
        public InputDataPort Divisor;
        public OutputDataPort Remainder;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Remainder, Value.FromObject(OperatorUtility.Modulo(
                ctx.ReadValue(Dividend).Box(),
                ctx.ReadValue(Divisor).Box())));
        }
    }
}
