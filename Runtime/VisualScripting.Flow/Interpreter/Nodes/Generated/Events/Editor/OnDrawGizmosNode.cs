// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.OnDrawGizmos))]
    public struct OnDrawGizmosNode : IEntryPointRegisteredNode<EmptyEventArgs>
    {
        public OutputTriggerPort Trigger;

        public bool Coroutine;
        public Type MessageListenerType => null;

        public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Trigger(Trigger, Coroutine);
            return Execution.Done;
        }

        public void Register<TCtx>(TCtx ctx, NodeId nodeId) where TCtx : IGraphInstance
        {
            ctx.RegisterEventHandler<Unity.VisualScripting.EmptyEventArgs>(nodeId, this, EventHooks.OnDrawGizmos, default);
        }

        public void AssignArguments<TCtx>(TCtx ctx, Unity.VisualScripting.EmptyEventArgs args) where TCtx : IGraphInstance
        {
        }
    }
}
