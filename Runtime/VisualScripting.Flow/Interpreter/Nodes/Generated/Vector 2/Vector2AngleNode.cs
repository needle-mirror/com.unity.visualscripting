// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector2Angle))]
    public struct Vector2AngleNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public OutputDataPort Angle;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Angle, Vector2.Angle(ctx.ReadVector2(A), ctx.ReadVector2(B)));
        }
    }
}
