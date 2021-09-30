using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(This))]
    [MovedFrom(false, sourceClassName: "SelfNode")]
    public struct ThisNode : IDataNode
    {
        public OutputDataPort Self;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Self, Value.FromObject(ctx.CurrentEntity));
        }
    }

    [NodeDescription(typeof(Null))]
    public struct NullNode : IConstantNode
    {
        public OutputDataPort Null;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Null, Value.FromObject(null));
        }
    }

    [NodeDescription(typeof(NullCheck))]
    public struct NullCheckNode : IFlowNode
    {
        public InputDataPort Input;
        public InputTriggerPort Enter;
        public OutputTriggerPort IfNotNull;
        public OutputTriggerPort IfNull;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ctx.Trigger(ctx.ReadObject<object>(Input) == null ? IfNull : IfNotNull);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(NullCoalesce))]
    public struct NullCoalesceNode : IFlowNode
    {
        public InputDataPort Input;
        public InputDataPort Fallback;
        public OutputDataPort Result;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var input = ctx.ReadObject<object>(Input);
            // Required cast because of Unity's custom == operator.
            bool isNull = input is UnityEngine.Object ? ((UnityEngine.Object)input == null) : input == null;
            ctx.Write(Result, isNull ? ctx.ReadValue(Fallback) : Value.FromObject(input));
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.GetVariable), SpecializationOf = VariableKind.Graph, UnmappedPorts = new[] { nameof(Unity.VisualScripting.GetVariable.name) })]
    public struct GetVariableNode : IDataNode//, INotFoldableNode
    {
        public OutputDataPort Value;
        public uint VariableHandle;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Value, ctx.GetGraphVariableValue(VariableHandle));
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.GetVariable), SpecializationOf = VariableKind.Object, UnmappedPorts = new[] { nameof(Unity.VisualScripting.GetVariable.name) })]
    public struct GetObjectVariableNode : IDataNode//, INotFoldableNode
    {
        public string VariableName;
        public InputDataPort Object;
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Value, Interpreter.Value.FromObject((ctx.ReadObject<GameObject>(Object) ?? ctx.CurrentEntity).GetComponent<Variables>().declarations.Get(VariableName)));
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.GetVariable), SpecializationOf = VariableKind.Scene, UnmappedPorts = new[] { nameof(Unity.VisualScripting.GetVariable.name) })]
    public struct GetSceneVariableNode : IDataNode//, INotFoldableNode
    {
        public string VariableName;
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Value, Interpreter.Value.FromObject(Variables.ActiveScene.Get(VariableName)));
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.GetVariable), SpecializationOf = VariableKind.Application, UnmappedPorts = new[] { nameof(Unity.VisualScripting.GetVariable.name) })]
    public struct GetApplicationVariableNode : IDataNode//, INotFoldableNode
    {
        public string VariableName;
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Value, Interpreter.Value.FromObject(Variables.Application.Get(VariableName)));
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.GetVariable), SpecializationOf = VariableKind.Saved, UnmappedPorts = new[] { nameof(Unity.VisualScripting.GetVariable.name) })]
    public struct GetSavedVariableNode : IDataNode//, INotFoldableNode
    {
        public string VariableName;
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Value, Interpreter.Value.FromObject(Variables.Saved.Get(VariableName)));
        }
    }


    [NodeDescription(typeof(Unity.VisualScripting.IsVariableDefined), UnmappedPorts = new[] { nameof(Unity.VisualScripting.IsVariableDefined.name) })]
    public struct IsVariableDefinedNode : IDataNode//, INotFoldableNode
    {
        public VariableKind Kind;
        public InputDataPort VariableName;
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var varName = ctx.ReadObject<string>(VariableName);
            bool isDefined;
            switch (Kind)
            {
                case VariableKind.Graph:
                    isDefined = true;
                    break;
                case VariableKind.Object:
                    isDefined = Variables.Object(ctx.CurrentEntity).IsDefined(varName);
                    break;
                case VariableKind.Scene:
                    isDefined = Variables.ActiveScene.IsDefined(varName);
                    break;
                case VariableKind.Application:
                    isDefined = Variables.Application.IsDefined(varName);
                    break;
                case VariableKind.Saved:
                    isDefined = Variables.Saved.IsDefined(varName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ;
            ctx.Write(Value, isDefined);
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.SetVariable), SpecializationOf = VariableKind.Graph, UnmappedPorts = new[] { nameof(Unity.VisualScripting.SetVariable.name) })]
    public struct SetVariableNode : IFlowNode
    {
        public InputTriggerPort Assign;
        public OutputTriggerPort Assigned;
        [PortDescription("input")]
        public InputDataPort NewValue;
        [PortDescription("output")]
        public OutputDataPort Value;

        public uint VariableHandle;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var newValue = ctx.ReadValue(NewValue);
            ctx.SetGraphVariableValue(VariableHandle, newValue);
            ctx.Write(Value, newValue);
            ctx.Trigger(Assigned);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.SetVariable), SpecializationOf = VariableKind.Object, UnmappedPorts = new[] { nameof(Unity.VisualScripting.SetVariable.name) })]
    public struct SetObjectVariableNode : IFlowNode
    {
        public string VariableName;
        public InputDataPort Object;

        public InputTriggerPort Assign;
        public OutputTriggerPort Assigned;
        [PortDescription("input")]
        public InputDataPort NewValue;
        [PortDescription("output")]
        public OutputDataPort Value;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var value = ctx.ReadValue(NewValue);
            (ctx.ReadObject<GameObject>(Object) ?? ctx.CurrentEntity).GetComponent<Variables>().declarations.Set(VariableName, value.Box());
            ctx.Write(Value, value);
            ctx.Trigger(Assigned);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.SetVariable), SpecializationOf = VariableKind.Scene, UnmappedPorts = new[] { nameof(Unity.VisualScripting.SetVariable.name) })]
    public struct SetSceneVariableNode : IFlowNode
    {
        public string VariableName;

        public InputTriggerPort Assign;
        public OutputTriggerPort Assigned;
        [PortDescription("input")]
        public InputDataPort NewValue;
        [PortDescription("output")]
        public OutputDataPort Value;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var value = ctx.ReadValue(NewValue);
            Variables.ActiveScene.Set(VariableName, value.Box());
            ctx.Write(Value, value);
            ctx.Trigger(Assigned);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.SetVariable), SpecializationOf = VariableKind.Application, UnmappedPorts = new[] { nameof(Unity.VisualScripting.SetVariable.name) })]
    public struct SetApplicationVariableNode : IFlowNode
    {
        public string VariableName;

        public InputTriggerPort Assign;
        public OutputTriggerPort Assigned;
        [PortDescription("input")]
        public InputDataPort NewValue;
        [PortDescription("output")]
        public OutputDataPort Value;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var value = ctx.ReadValue(NewValue);
            Variables.Application.Set(VariableName, value.Box());
            ctx.Write(Value, value);
            ctx.Trigger(Assigned);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.SetVariable), SpecializationOf = VariableKind.Saved, UnmappedPorts = new[] { nameof(Unity.VisualScripting.SetVariable.name) })]
    public struct SetSavedVariableNode : IFlowNode
    {
        public string VariableName;

        public InputTriggerPort Assign;
        public OutputTriggerPort Assigned;
        [PortDescription("input")]
        public InputDataPort NewValue;
        [PortDescription("output")]
        public OutputDataPort Value;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var value = ctx.ReadValue(NewValue);
            Variables.Saved.Set(VariableName, value.Box());
            ctx.Write(Value, value);
            ctx.Trigger(Assigned);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(Unity.VisualScripting.TriggerCustomEvent), UnmappedPorts = new[] { nameof(Unity.VisualScripting.SetVariable.name) })]
    public struct TriggerCustomEventNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        [PortDescription("name")]
        public InputDataPort Name;
        [PortDescription("target")]
        public InputDataPort Target;
        public InputDataMultiPort Arguments;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var target = ctx.ReadObject<GameObject>(Target) ?? ctx.CurrentEntity;
            var eventName = ctx.ReadObject<string>(Name);
            object[] arguments = new object[Arguments.DataCount];
            for (uint i = 0; i < Arguments.DataCount; i++)
            {
                arguments[i] = ctx.ReadValue(Arguments.SelectPort(i)).Box();
            }

            CustomEvent.Trigger(target, eventName, arguments);

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(SaveVariables))]
    public struct SaveVariablesNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            SavedVariables.SaveDeclarations(SavedVariables.merged);
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }

    [MemberNode(typeof(InvokeMember), "UnityEngine.Debug.Log(System.Object)")]
    public struct LogNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        [PortDescription("%message")]
        public InputDataPort Message;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var message = ctx.ReadValue(Message);
            Debug.Log($"{message.ToPrettyString()}");
            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
