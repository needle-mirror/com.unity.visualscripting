// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector2Divide))]
    public struct Vector2DivideNode : IDataNode, IFoldableNode
    {
        public InputDataPort Dividend;
        public InputDataPort Divisor;
        public OutputDataPort Quotient;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var dividend = ctx.ReadVector2(Dividend);
            var divisor = ctx.ReadVector2(Divisor);
            ctx.Write(Quotient, new Vector2(dividend.x / divisor.x, dividend.y / divisor.y));
        }
    }
}
