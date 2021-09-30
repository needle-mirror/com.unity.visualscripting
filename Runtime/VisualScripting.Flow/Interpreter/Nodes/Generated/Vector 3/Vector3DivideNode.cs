// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector3Divide))]
    public struct Vector3DivideNode : IDataNode, IFoldableNode
    {
        public InputDataPort Dividend;
        public InputDataPort Divisor;
        public OutputDataPort Quotient;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var dividend = ctx.ReadVector3(Dividend);
            var divisor = ctx.ReadVector3(Divisor);
            ctx.Write(Quotient, new Vector3(dividend.x / divisor.x, dividend.y / divisor.y, dividend.z / divisor.z));
        }
    }
}
