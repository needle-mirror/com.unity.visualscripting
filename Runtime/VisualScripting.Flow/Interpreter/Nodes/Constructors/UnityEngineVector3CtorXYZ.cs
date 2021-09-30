// GENERATED FILE copied because it's often used
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [MemberNodeAttribute(typeof(Unity.VisualScripting.InvokeMember), "UnityEngine.Vector3..ctor(System.SingleSystem.SingleSystem.Single)")]
    public struct UnityEngineVector3CtorXYZ : IDataNode
    {
        [PortDescription("result")]
        public OutputDataPort Result;
        [PortDescription("%x")]
        public InputDataPort x;
        [PortDescription("%y")]
        public InputDataPort y;
        [PortDescription("%z")]
        public InputDataPort z;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Result, new UnityEngine.Vector3(ctx.ReadFloat(x), ctx.ReadFloat(y), ctx.ReadFloat(z)));
        }
    }
}
