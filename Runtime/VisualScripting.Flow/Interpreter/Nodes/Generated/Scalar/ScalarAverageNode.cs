// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.ScalarAverage))]
    public struct ScalarAverageNode : IDataNode, IFoldableNode
    {
        public InputDataMultiPort Inputs;
        public OutputDataPort Average;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var result = ctx.ReadFloat(Inputs.SelectPort(0));
            for (uint i = 1; i < Inputs.DataCount; i++)
                result = result + ctx.ReadFloat(Inputs.SelectPort(i));
            ctx.Write(Average, result / Inputs.DataCount);
        }
    }
}
