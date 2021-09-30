using Unity.VisualScripting;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Vector2Project))]
    public struct Vector2ProjectNode : IDataNode
    {
        public InputDataPort A;
        public InputDataPort B;
        public OutputDataPort Projection;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var b = ctx.ReadVector2(B);
            ctx.Write(Projection, Vector2.Dot(ctx.ReadVector2(A), b) * b.normalized);
        }
    }
}
