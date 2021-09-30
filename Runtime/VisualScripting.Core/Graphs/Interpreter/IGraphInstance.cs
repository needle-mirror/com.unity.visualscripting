using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    public struct TimeData
    {
        public float DeltaTime;
        public float UnscaledDeltaTime;
    }

    [Serializable]
    public struct ScriptGraphAssetIndex
    {
        public int Index;
    }

    public interface IGraphInstance
    {
        GameObject CurrentEntity { get; }
        TimeData TimeData { get; }
        // ReSharper disable once UnusedParameter.Global
        ref TS GetState<T, TS>(in T _) where T : IStatefulNode<TS> where TS : unmanaged, INodeState;
        ref TS GetCoroutineState<T, TS>(in T _) where T : ICoroutineStatefulNode<TS> where TS : unmanaged, INodePerCoroutineState;

        void RegisterEventHandler<T>(NodeId id, IEntryPointRegisteredNode<T> node, string hookName, InputDataPort targetGameObjectPort);

        ReflectedMember GetReflectedMember(uint memberIndex);

        /// <summary>
        /// Trigger execution from a Trigger output node
        /// </summary>
        /// <param name="output">Output Trigger Node to execute from</param>
        /// <param name="asCoroutine"></param>
        void Trigger(OutputTriggerPort output, bool asCoroutine = false);

        void Write(OutputDataPort port, Value value);

        int CurrentLoopId { get; }
        int StartLoop();
        void EndLoop(int loopId);
        void BreakCurrentLoop();

        Value GetGraphVariableValue(uint dataIndex);
        void SetGraphVariableValue(uint dataIndex, Value value);

        OutputDataPort GetPulledDataPort();

        bool ReadBool(InputDataPort port);
        int ReadInt(InputDataPort port);
        float ReadFloat(InputDataPort port);
        Vector2 ReadVector2(InputDataPort port);
        Vector3 ReadVector3(InputDataPort port);
        Vector4 ReadVector4(InputDataPort port);
        Color ReadColor(InputDataPort port);
        Quaternion ReadQuaternion(InputDataPort port);
        T ReadObject<T>(InputDataPort port) where T : class;
        T ReadStruct<T>(InputDataPort port) where T : struct;
        Value ReadValue(InputDataPort port);
    }
}
