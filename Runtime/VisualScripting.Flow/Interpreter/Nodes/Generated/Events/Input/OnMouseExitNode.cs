// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.OnMouseExit))]
    public struct OnMouseExitNode : IEntryPointRegisteredNode<EmptyEventArgs>
    {
        public OutputTriggerPort Trigger;
        [PortDescription("target")]
        public InputDataPort Target;

        public bool Coroutine;
        public Type MessageListenerType => typeof(Unity.VisualScripting.UnityOnMouseExitMessageListener);

        public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Trigger(Trigger, Coroutine);
            return Execution.Done;
        }

        public void Register<TCtx>(TCtx ctx, NodeId nodeId) where TCtx : IGraphInstance
        {
            ctx.RegisterEventHandler<Unity.VisualScripting.EmptyEventArgs>(nodeId, this, EventHooks.OnMouseExit, Target);
        }

        public void AssignArguments<TCtx>(TCtx ctx, Unity.VisualScripting.EmptyEventArgs args) where TCtx : IGraphInstance
        {
        }
    }
}
