// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.Vector2Multiply))]
    public struct Vector2MultiplyNode : IDataNode, IFoldableNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public OutputDataPort Product;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var a = ctx.ReadVector2(A);
            var b = ctx.ReadVector2(B);
            ctx.Write(Product, new Vector2(a.x * b.x, a.y * b.y));
        }
    }
}
