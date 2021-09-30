// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector4Distance))]
    public struct Vector4DistanceNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public OutputDataPort Distance;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Distance, Vector4.Distance(ctx.ReadVector4(A), ctx.ReadVector4(B)));
        }
    }
}
