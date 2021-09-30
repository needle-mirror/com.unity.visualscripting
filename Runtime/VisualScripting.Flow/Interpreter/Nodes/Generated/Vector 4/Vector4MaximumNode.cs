// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector4Maximum))]
    public struct Vector4MaximumNode : IDataNode, IFoldableNode
    {
        public InputDataMultiPort Inputs;
        public OutputDataPort Maximum;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var result = ctx.ReadVector4(Inputs.SelectPort(0));
            for (uint i = 1; i < Inputs.DataCount; i++)
                result = Vector4.Max(result, ctx.ReadVector4(Inputs.SelectPort(i)));
            ctx.Write(Maximum, result);
        }
    }
}
