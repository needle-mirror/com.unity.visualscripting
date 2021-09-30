using System;
using Unity.VisualScripting;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(Unity.VisualScripting.CustomEvent))]
    public struct CustomEventNode : IEntryPointRegisteredNode<CustomEventArgs>
    {
        public bool Coroutine;
        public OutputTriggerPort Trigger;
        public InputDataPort Name;
        public InputDataPort Target;// target port
        public OutputDataMultiPort Arguments;

        public Type MessageListenerType => null;

        public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Trigger(Trigger, Coroutine);
            return Execution.Done;
        }

        public void Register<TCtx>(TCtx ctx, NodeId nodeId) where TCtx : IGraphInstance
        {
            ctx.RegisterEventHandler<Unity.VisualScripting.CustomEventArgs>(nodeId, this, EventHooks.Custom, Target);
        }

        public void AssignArguments<TCtx>(TCtx ctx, Unity.VisualScripting.CustomEventArgs args) where TCtx : IGraphInstance
        {
            for (uint i = 0; i < args.arguments.Length; i++)
            {
                ctx.Write(Arguments.SelectPort(i), Value.FromObject(args.arguments[i]));
            }
        }
    }
}
