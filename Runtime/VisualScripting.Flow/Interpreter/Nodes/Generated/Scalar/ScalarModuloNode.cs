// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.ScalarModulo))]
    public struct ScalarModuloNode : IDataNode, IFoldableNode
    {
        public InputDataPort Dividend;
        public InputDataPort Divisor;
        public OutputDataPort Remainder;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Remainder, ctx.ReadFloat(Dividend) % ctx.ReadFloat(Divisor));
        }
    }
}
