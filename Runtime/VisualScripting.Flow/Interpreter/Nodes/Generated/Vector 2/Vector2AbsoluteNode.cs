// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector2Absolute))]
    public struct Vector2AbsoluteNode : IDataNode, IFoldableNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector2(Input);
            ctx.Write(Output, new Vector2(Mathf.Abs(input.x), Mathf.Abs(input.y)));
        }
    }
}
