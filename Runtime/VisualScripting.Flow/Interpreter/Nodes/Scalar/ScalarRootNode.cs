using Unity.VisualScripting;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(ScalarRoot))]
    public struct ScalarRootNode : IDataNode
    {
        public InputDataPort Radicand;
        public InputDataPort Degree;
        public OutputDataPort Root;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var radicand = ctx.ReadFloat(Radicand);
            var degree = ctx.ReadFloat(Degree);
            // TODO split node in two, Root and Sqrt, during translation
            ctx.Write(Root, degree == 2 ? Mathf.Sqrt(radicand) : Mathf.Pow(radicand, 1 / degree));
        }
    }
}
