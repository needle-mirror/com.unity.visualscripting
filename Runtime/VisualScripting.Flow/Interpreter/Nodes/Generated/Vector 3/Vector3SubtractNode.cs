// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector3Subtract))]
    public struct Vector3SubtractNode : IDataNode, IFoldableNode
    {
        public InputDataPort Minuend;
        public InputDataPort Subtrahend;
        public OutputDataPort Difference;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Difference, ctx.ReadVector3(Minuend) - ctx.ReadVector3(Subtrahend));
        }
    }
}
