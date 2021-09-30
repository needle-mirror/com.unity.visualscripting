// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.ScalarExponentiate))]
    public struct ScalarExponentiateNode : IDataNode, IFoldableNode
    {
        public InputDataPort Base;
        public InputDataPort Exponent;
        public OutputDataPort Power;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Power, Mathf.Pow(ctx.ReadFloat(Base), ctx.ReadFloat(Exponent)));
        }
    }
}
