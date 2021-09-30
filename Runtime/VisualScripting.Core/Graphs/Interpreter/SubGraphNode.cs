using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    public struct SubGraphNode : IFlowNode, IDataNode
    {
        public ScriptGraphAssetIndex NestedGraphAssetIndex;
        public InputTriggerMultiPort InputTriggers;
        public OutputTriggerMultiPort OutputTriggers;
        public InputDataMultiPort InputDatas;
        public OutputDataMultiPort OutputDatas;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ((GraphInstance)(IGraphInstance)ctx).TriggerNestedGraphInput(NestedGraphAssetIndex, InputTriggers.GetSubPortIndex(port));
            return Execution.Done;
        }

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            OutputDataPort pulledDataPort = ctx.GetPulledDataPort();
            var val = ((GraphInstance)(IGraphInstance)ctx).PullNestedGraphDataOutput(NestedGraphAssetIndex, OutputDatas.GetSubPortIndex(pulledDataPort));
            ctx.Write(pulledDataPort, val);
        }
    }

    public struct GraphInputNode : IDataNode
    {
        public OutputTriggerMultiPort Triggers;
        public OutputDataMultiPort Datas;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            OutputDataPort pulledDataPort = ctx.GetPulledDataPort();
            var val = ((GraphInstance)(IGraphInstance)ctx).PullParentGraphDataInput(Datas.GetSubPortIndex(pulledDataPort));
            if (val.Type != ValueType.Unknown)
            {
                ctx.Write(pulledDataPort, val);
            }
        }
    }

    public struct GraphOutputNode : IFlowNode
    {
        public InputTriggerMultiPort Triggers;
        public InputDataMultiPort Datas;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ((GraphInstance)(IGraphInstance)ctx).TriggerParentGraphOutput(Triggers.GetSubPortIndex(port));
            return Execution.Done;
        }
    }
}
