// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector3Lerp))]
    public struct Vector3LerpNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public InputDataPort T;
        public OutputDataPort Interpolation;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Interpolation, Vector3.Lerp(ctx.ReadVector3(A), ctx.ReadVector3(B), ctx.ReadFloat(T)));
        }
    }
}
