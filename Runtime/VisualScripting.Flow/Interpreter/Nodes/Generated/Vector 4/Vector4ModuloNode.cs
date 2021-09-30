// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector4Modulo))]
    public struct Vector4ModuloNode : IDataNode, IFoldableNode
    {
        public InputDataPort Dividend;
        public InputDataPort Divisor;
        public OutputDataPort Remainder;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var dividend = ctx.ReadVector4(Dividend);
            var divisor = ctx.ReadVector4(Divisor);
            ctx.Write(Remainder, new Vector4(dividend.x % divisor.x, dividend.y % divisor.y, dividend.z % divisor.z, dividend.w % divisor.w));
        }
    }
}
