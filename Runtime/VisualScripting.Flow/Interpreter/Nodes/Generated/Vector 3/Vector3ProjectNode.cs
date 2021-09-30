// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector3Project))]
    public struct Vector3ProjectNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public OutputDataPort Projection;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Projection, Vector3.Project(ctx.ReadVector3(A), ctx.ReadVector3(B)));
        }
    }
}
