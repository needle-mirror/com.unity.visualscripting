// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.ScalarLerp))]
    public struct ScalarLerpNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public InputDataPort T;
        public OutputDataPort Interpolation;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Interpolation, Mathf.Lerp(ctx.ReadFloat(A), ctx.ReadFloat(B), ctx.ReadFloat(T)));
        }
    }
}
