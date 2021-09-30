using System;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    public struct GetMemberReflectionNode : IDataNode, IReflectedMemberNode
    {
        uint IReflectedMemberNode.ReflectedMemberIndex => ReflectedMemberIndex;

        public InputDataPort Target;
        public OutputDataPort Value;
        public uint ReflectedMemberIndex;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var member = ctx.GetReflectedMember(ReflectedMemberIndex);
            member.EnsureReflected();
            var target = member.RequiresTarget ? ctx.ReadValue(Target).Box(member.TargetType) : null;
            ctx.Write(Value, Interpreter.Value.FromObject(member.Get(target)));
        }
    }
    public struct SetMemberReflectionNode : IFlowNode
    {
        public uint ReflectedMemberIndex;
        public InputTriggerPort Assign;
        public OutputTriggerPort Assigned;
        public InputDataPort Input;
        public OutputDataPort Output;
        public InputDataPort Target;
        public OutputDataPort TargetOutput;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ReflectedMember member = ctx.GetReflectedMember(ReflectedMemberIndex);
            member.EnsureReflected();
            object target;
            if (member.RequiresTarget)
            {
                var targetValue = ctx.ReadValue(Target);
                target = targetValue.Object(member.TargetType);
                ctx.Write(TargetOutput, targetValue);
            }
            else
                target = null;
            var val = ctx.ReadValue(Input);
            member.Set(target, val.Box());
            ctx.Write(Output, val);
            ctx.Trigger(Assigned);
            return Execution.Done;
        }
    }
    public struct InvokeMemberReflectionNode : IDataNode, IReflectedMemberNode
    {
        uint IReflectedMemberNode.ReflectedMemberIndex => ReflectedMemberIndex;

        public uint ReflectedMemberIndex;
        public InputDataMultiPort Input;
        public OutputDataMultiPort Output;
        public OutputDataPort Result;
        public InputDataPort Target;
        public OutputDataPort TargetOutput;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ReflectedMember member = ctx.GetReflectedMember(ReflectedMemberIndex);
            member.EnsureReflected();
            object target;
            if (member.RequiresTarget)
            {
                var targetValue = ctx.ReadValue(Target);
                target = targetValue.Object(member.TargetType);
                ctx.Write(TargetOutput, targetValue);
            }
            else
                target = null;


            var args = InvokeMemberVoidReflectionNode.MakeArgumentArray(Input, ctx, member);

            var result = member.Invoke(target, args);
            if (member.IsGettable)
                ctx.Write(Result, Value.FromObject(result));

            InvokeMemberVoidReflectionNode.CopyRefAndOutParametersToNodeOutputs(ctx, member, Output, args);

        }
    }
    public struct InvokeMemberVoidReflectionNode : IFlowNode
    {
        public uint ReflectedMemberIndex;
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public InputDataMultiPort Input;
        public OutputDataMultiPort Output;
        public InputDataPort Target;
        public OutputDataPort TargetOutput;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ReflectedMember member = ctx.GetReflectedMember(ReflectedMemberIndex);
            member.EnsureReflected();
            object target;
            if (member.RequiresTarget)
            {
                var targetValue = ctx.ReadValue(Target);
                target = targetValue.Object(member.TargetType);
                ctx.Write(TargetOutput, targetValue);
            }
            else
                target = null;

            var args = MakeArgumentArray(Input, ctx, member);

            member.Invoke(target, args);

            CopyRefAndOutParametersToNodeOutputs(ctx, member, Output, args);

            ctx.Trigger(Exit);
            return Execution.Done;
        }

        static internal void CopyRefAndOutParametersToNodeOutputs<TCtx>(TCtx ctx, ReflectedMember member, OutputDataMultiPort output, object[] args)
            where TCtx : IGraphInstance
        {
            uint nextOutput = 0;
            for (var i = 0; i < member.ParameterModifiers.Length; i++)
            {
                if (member.ParameterModifiers[i] != ReflectedMember.ParameterModifier.None)
                    ctx.Write(output.SelectPort(nextOutput++), Value.FromObject(args[i]));
            }
        }

        internal static object[] MakeArgumentArray<TCtx>(InputDataMultiPort inputDataMultiPort, TCtx ctx, ReflectedMember member) where TCtx : IGraphInstance
        {
            object[] args = new object[member.ParameterTypes.Length];
            uint portIndex = 0;
            for (uint i = 0; i < member.ParameterTypes.Length; i++)
            {
                if (member.ParameterModifiers[i] != ReflectedMember.ParameterModifier.Out)
                    args[i] = ctx.ReadValue(inputDataMultiPort.SelectPort(portIndex++)).Box(member.ParameterTypes[i]);

            }

            return args;
        }
    }
    // public struct InvokeMemberReflectionNodeData : IDataNode
    // {
    //     public uint ReflectedMemberIndex;
    //     public InputDataMultiPort Input;
    //     public OutputDataMultiPort Output;
    //     public OutputDataPort Result;
    //     public InputDataPort Target;
    //     public OutputDataPort TargetOutput;
    //
    //     public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
    //     {
    //         Member member = ctx.GetReflectedMember(ReflectedMemberIndex);
    //         member.EnsureReflected();
    //         object target;
    //         if (member.requiresTarget)
    //         {
    //             var targetValue = ctx.ReadValue(Target);
    //             target = targetValue.Object(member.targetType);
    //             ctx.Write(TargetOutput, targetValue);
    //         }
    //         else
    //             target = null;
    //
    //         object[] args = new object[Input.DataCount];
    //         for (uint i = 0; i < Input.DataCount; i++)
    //         {
    //             args[i] = ctx.ReadValue(Input.SelectPort(i)).Box();
    //         }
    //
    //         var result = member.Invoke(target, args);
    //         if (member.isGettable)
    //             ctx.Write(Result, Value.FromObject(result));
    //     }
    // }
}
