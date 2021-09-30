// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector4Multiply))]
    public struct Vector4MultiplyNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public OutputDataPort Product;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var a = ctx.ReadVector4(A);
            var b = ctx.ReadVector4(B);
            ctx.Write(Product, new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w));
        }
    }
}
