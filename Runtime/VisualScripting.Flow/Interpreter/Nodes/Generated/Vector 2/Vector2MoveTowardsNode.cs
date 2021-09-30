// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector2MoveTowards))]
    public struct Vector2MoveTowardsNode : IDataNode, IFoldableNode
    {
        public InputDataPort Current;
        public InputDataPort Target;
        public InputDataPort MaxDelta;
        public OutputDataPort Result;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Result, Vector2.MoveTowards(ctx.ReadVector2(Current), ctx.ReadVector2(Target), ctx.ReadFloat(MaxDelta)));
        }
    }
}
