using Unity.VisualScripting;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    // [EventNodeDescriptionAttribute(typeof(Update), EventHooks.Update)]
    // public struct UpdateNode : IEntryPointNode
    // {
    //     public OutputTriggerPort Trigger;
    //
    //     public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
    //     {
    //         ctx.Trigger(Trigger);
    //         return Execution.Done;
    //     }
    // }
    // [EventNodeDescriptionAttribute(typeof(FixedUpdate), EventHooks.FixedUpdate)]
    // public struct FixedUpdateNode : IEntryPointNode
    // {
    //     public OutputTriggerPort Trigger;
    //
    //     public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
    //     {
    //         ctx.Trigger(Trigger);
    //         return Execution.Done;
    //     }
    // }
    // [EventNodeDescriptionAttribute(typeof(Start), EventHooks.Start)]
    // public struct StartNode : IEntryPointNode
    // {
    //     public OutputTriggerPort Trigger;
    //
    //     public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
    //     {
    //         ctx.Trigger(Trigger);
    //         return Execution.Done;
    //     }
    // }

    // [NodeDescription(typeof(OnKeyboardInputNode))]
    // public struct OnKeyboardInput : IEntryPointNode
    // {
    //     public OutputTriggerPort Trigger;
    //     public InputDataPort Key;
    //     public InputDataPort Action;
    //
    //     public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
    //     {
    //         int enumValue = ctx.ReadValueOfType(Key, ValueType.Enum).EnumValue;
    //         var key = (KeyCode)enumValue;
    //         var action = (PressState)(ctx.ReadValueOfType(Action, ValueType.Enum).EnumValue);
    //
    //         switch (action)
    //         {
    //             case PressState.Down:
    //                 if (Input.GetKeyDown(key)) ctx.Trigger(Trigger);
    //                 break;
    //             case PressState.Up:
    //                 if (Input.GetKeyUp(key)) ctx.Trigger(Trigger);
    //                 break;
    //             case PressState.Hold:
    //                 if (Input.GetKey(key)) ctx.Trigger(Trigger);
    //                 break;
    //             default: throw new UnexpectedEnumValueException<PressState>(action);
    //         }
    //
    //         return Execution.Done;
    //     }
    // }
}
