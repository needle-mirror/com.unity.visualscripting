// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.ScalarDivide))]
    public struct ScalarDivideNode : IDataNode, IFoldableNode
    {
        public InputDataPort Dividend;
        public InputDataPort Divisor;
        public OutputDataPort Quotient;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Quotient, ctx.ReadFloat(Dividend) / ctx.ReadFloat(Divisor));
        }
    }
}
