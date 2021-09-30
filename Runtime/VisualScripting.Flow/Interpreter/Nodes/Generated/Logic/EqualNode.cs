// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Equal))]
    public struct EqualNode : IDataNode, IFoldableNode
    {
        [PortDescription("equal")]
        public OutputDataPort Comparison;
        public InputDataPort A;
        public InputDataPort B;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Comparison, Value.Equals(ctx.ReadValue(A), ctx.ReadValue(B)));
        }
    }
}
