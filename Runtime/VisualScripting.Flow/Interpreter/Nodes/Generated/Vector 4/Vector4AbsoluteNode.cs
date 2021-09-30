// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector4Absolute))]
    public struct Vector4AbsoluteNode : IDataNode, IFoldableNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector4(Input);
            ctx.Write(Output, new Vector4(Mathf.Abs(input.x), Mathf.Abs(input.y), Mathf.Abs(input.z), Mathf.Abs(input.w)));
        }
    }
}
