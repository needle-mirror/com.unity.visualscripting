// GENERATED FILE copied because it's often used
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [MemberNodeAttribute(typeof(Unity.VisualScripting.InvokeMember), "UnityEngine.Vector4..ctor(System.SingleSystem.SingleSystem.SingleSystem.Single)")]
    public struct UnityEngineVector4CtorXYZW : IDataNode
    {
        [PortDescription("result")]
        public OutputDataPort Result;
        [PortDescription("%x")]
        public InputDataPort x;
        [PortDescription("%y")]
        public InputDataPort y;
        [PortDescription("%z")]
        public InputDataPort z;
        [PortDescription("%w")]
        public InputDataPort w;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Result, new UnityEngine.Vector4(ctx.ReadFloat(x), ctx.ReadFloat(y), ctx.ReadFloat(z), ctx.ReadFloat(w)));
        }
    }
}
