// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.OnJointBreak))]
    public struct OnJointBreakNode : IEntryPointRegisteredNode<float>
    {
        public OutputTriggerPort Trigger;
        [PortDescription("target")]
        public InputDataPort Target;
        public OutputDataPort BreakForce;

        public bool Coroutine;
        public Type MessageListenerType => typeof(Unity.VisualScripting.UnityOnJointBreakMessageListener);

        public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Trigger(Trigger, Coroutine);
            return Execution.Done;
        }

        public void Register<TCtx>(TCtx ctx, NodeId nodeId) where TCtx : IGraphInstance
        {
            ctx.RegisterEventHandler<float>(nodeId, this, EventHooks.OnJointBreak, Target);
        }

        public void AssignArguments<TCtx>(TCtx ctx, float args) where TCtx : IGraphInstance
        {
            // regionStart='assign'
            ctx.Write(BreakForce, args);
            // regionEnd='assign'
        }
    }
}
