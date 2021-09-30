// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector4MoveTowards))]
    public struct Vector4MoveTowardsNode : IDataNode, IFoldableNode
    {
        public InputDataPort Current;
        public InputDataPort Target;
        public InputDataPort MaxDelta;
        public OutputDataPort Result;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Result, Vector4.MoveTowards(ctx.ReadVector4(Current), ctx.ReadVector4(Target), ctx.ReadFloat(MaxDelta)));
        }
    }
}
