#define VS_TRACING

using System;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

#if VS_TRACING
public class DotsFrameTrace
{
    public enum StepType : byte
    {
        None,
        ExecutedNode,
        TriggeredPort,
        WrittenValue,
        ReadValue,
        Error
    }

    public struct RecordedStep
    {
        public StepType Type;
        public NodeId NodeId;
        public Port Port;
        public Value Value;
        public byte Progress;
        public Exception ErrorMessage;

        public static RecordedStep ExecutedNode(NodeId nodeId, byte progress)
        {
            return new RecordedStep
            {
                Type = StepType.ExecutedNode,
                NodeId = nodeId,
                Progress = progress,
            };
        }

        public static RecordedStep TriggeredPort(OutputTriggerPort outputPort) => new RecordedStep
        {
            Type = StepType.TriggeredPort,
            Port = outputPort.Port,
        };

        public static RecordedStep ReadValue(InputDataPort inputDataPort, Value value) => new RecordedStep
        {
            Type = StepType.ReadValue,
            Port = inputDataPort.Port,
            Value = value,
        };

        public static RecordedStep WrittenValue(OutputDataPort outputDataPort, Value value) => new RecordedStep
        {
            Type = StepType.WrittenValue,
            Port = outputDataPort.Port,
            Value = value,
        };

        public static RecordedStep Error(NodeId nodeId, Exception message) => new RecordedStep
        {
            Type = StepType.Error,
            NodeId = nodeId,
            ErrorMessage = message,
        };
    }


    public static System.Action<uint, int, GameObject, RecordedStep> OnRecordFrameTraceDelegate;
    public uint hash;
    public int frameCount;
    public GameObject entity;
    [System.Diagnostics.Conditional("VS_TRACING")]
    public void RecordExecutedNode(NodeId nodeId, byte progress)
    {
        OnRecordFrameTraceDelegate?.Invoke(hash, frameCount, entity, RecordedStep.ExecutedNode(nodeId, progress));
    }

    [System.Diagnostics.Conditional("VS_TRACING")]
    public void RecordTriggeredPort(OutputTriggerPort output)
    {
        OnRecordFrameTraceDelegate?.Invoke(hash, frameCount, entity, RecordedStep.TriggeredPort(output));
    }

    [System.Diagnostics.Conditional("VS_TRACING")]
    public void RecordReadValue(Value value, InputDataPort port)
    {
        OnRecordFrameTraceDelegate?.Invoke(hash, frameCount, entity, RecordedStep.ReadValue(port, value));
    }

    [System.Diagnostics.Conditional("VS_TRACING")]
    public void RecordWrittenValue(Value value, OutputDataPort port)
    {
        OnRecordFrameTraceDelegate?.Invoke(hash, frameCount, entity, RecordedStep.WrittenValue(port, value));
    }

    [System.Diagnostics.Conditional("VS_TRACING")]
    public void RecordError(NodeId nodeId, Exception exception)
    {
        OnRecordFrameTraceDelegate?.Invoke(hash, frameCount, entity, RecordedStep.Error(nodeId, exception));
    }
}
#endif
