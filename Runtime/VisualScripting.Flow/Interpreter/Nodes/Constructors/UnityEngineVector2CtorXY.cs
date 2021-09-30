// GENERATED FILE copied because it's often used
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [MemberNodeAttribute(typeof(Unity.VisualScripting.InvokeMember), "UnityEngine.Vector2..ctor(System.SingleSystem.Single)")]
    public struct UnityEngineVector2CtorXY : IDataNode
    {
        [PortDescription("result")]
        public OutputDataPort Result;
        [PortDescription("%x")]
        public InputDataPort x;
        [PortDescription("%y")]
        public InputDataPort y;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Result, new UnityEngine.Vector2(ctx.ReadFloat(x), ctx.ReadFloat(y)));
        }
    }
}
