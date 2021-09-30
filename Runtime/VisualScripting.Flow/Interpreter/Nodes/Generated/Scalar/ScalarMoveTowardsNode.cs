// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.ScalarMoveTowards))]
    public struct ScalarMoveTowardsNode : IDataNode, IFoldableNode
    {
        public InputDataPort Current;
        public InputDataPort Target;
        public InputDataPort MaxDelta;
        public OutputDataPort Result;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Result, Mathf.MoveTowards(ctx.ReadFloat(Current), ctx.ReadFloat(Target), ctx.ReadFloat(MaxDelta)));
        }
    }
}
