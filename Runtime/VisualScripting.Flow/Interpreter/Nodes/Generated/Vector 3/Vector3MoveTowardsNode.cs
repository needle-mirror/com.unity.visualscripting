// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector3MoveTowards))]
    public struct Vector3MoveTowardsNode : IDataNode, IFoldableNode
    {
        public InputDataPort Current;
        public InputDataPort Target;
        public InputDataPort MaxDelta;
        public OutputDataPort Result;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Result, Vector3.MoveTowards(ctx.ReadVector3(Current), ctx.ReadVector3(Target), ctx.ReadFloat(MaxDelta)));
        }
    }
}
