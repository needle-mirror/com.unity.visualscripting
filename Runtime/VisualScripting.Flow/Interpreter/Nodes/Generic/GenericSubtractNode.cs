using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(GenericSubtract))]
    public struct GenericSubtractNode : IDataNode, IFoldableNode
    {
        public InputDataPort Minuend;
        public InputDataPort Subtrahend;
        public OutputDataPort Difference;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Difference, Value.FromObject(OperatorUtility.Subtract(
                ctx.ReadValue(Minuend).Box(),
                ctx.ReadValue(Subtrahend).Box())));
        }
    }
}
