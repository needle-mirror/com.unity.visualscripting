// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.OnDrop))]
    public struct OnDropNode : IEntryPointRegisteredNode<UnityEngine.EventSystems.PointerEventData>
    {
        public OutputTriggerPort Trigger;
        [PortDescription("target")]
        public InputDataPort Target;
        public OutputDataPort Data;

        public bool Coroutine;
        public Type MessageListenerType => typeof(Unity.VisualScripting.UnityOnDropMessageListener);

        public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Trigger(Trigger, Coroutine);
            return Execution.Done;
        }

        public void Register<TCtx>(TCtx ctx, NodeId nodeId) where TCtx : IGraphInstance
        {
            ctx.RegisterEventHandler<UnityEngine.EventSystems.PointerEventData>(nodeId, this, EventHooks.OnDrop, Target);
        }

        public void AssignArguments<TCtx>(TCtx ctx, UnityEngine.EventSystems.PointerEventData args) where TCtx : IGraphInstance
        {
            // regionStart='assign'
            ctx.Write(Data, Value.FromObject(args));
            // regionEnd='assign'
        }
    }
}
