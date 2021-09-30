// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector2Minimum))]
    public struct Vector2MinimumNode : IDataNode, IFoldableNode
    {
        public InputDataMultiPort Inputs;
        public OutputDataPort Minimum;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var result = ctx.ReadVector2(Inputs.SelectPort(0));
            for (uint i = 1; i < Inputs.DataCount; i++)
                result = Vector2.Min(result, ctx.ReadVector2(Inputs.SelectPort(i)));
            ctx.Write(Minimum, result);
        }
    }
}
