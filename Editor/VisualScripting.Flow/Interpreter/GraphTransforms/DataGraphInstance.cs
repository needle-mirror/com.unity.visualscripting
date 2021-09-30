using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    struct DataGraphInstance : IGraphInstance
    {
        private Dictionary<uint, Value> m_Values;
        private ILookup<uint, uint> m_OutputPortToConnectedInputPorts;
        private readonly GraphBuilder m_GraphBuilder;

        public TimeData TimeData => throw new InvalidDataException();

        public DataGraphInstance(Dictionary<uint, Value> mValues, ILookup<uint, uint> mOutputPortToConnectedInputPorts,
                                 GraphBuilder graphBuilder) : this()
        {
            m_Values = mValues;
            m_OutputPortToConnectedInputPorts = mOutputPortToConnectedInputPorts;
            m_GraphBuilder = graphBuilder;
        }

        private bool ReadValue(InputDataPort port, out Value value) => m_Values.TryGetValue(port.Port.Index, out value);
        public void Write(OutputDataPort port, Value value)
        {
            m_Values[port.Port.Index] = value;
            foreach (var inputPortIndex in m_OutputPortToConnectedInputPorts[port.Port.Index])
            {
                m_Values[inputPortIndex] = value;
            }
        }

        public GameObject CurrentEntity { get; }
        public ref TS GetState<T, TS>(in T _) where T : IStatefulNode<TS> where TS : unmanaged, INodeState => throw new InvalidOperationException();
        public ref TS GetCoroutineState<T, TS>(in T _) where T : ICoroutineStatefulNode<TS> where TS : unmanaged, INodePerCoroutineState
        {
            throw new NotImplementedException();
        }

        public void RegisterEventHandler<T>(NodeId id, IEntryPointRegisteredNode<T> node, string hookName,
            InputDataPort targetGameObjectPort) =>
            throw new InvalidOperationException();

        public ReflectedMember GetReflectedMember(uint memberIndex) => new ReflectedMember(m_GraphBuilder.GetReflectedMember(memberIndex));

        public void Trigger(OutputTriggerPort output, bool asCoroutine) => throw new InvalidOperationException();

        public int CurrentLoopId { get; }
        public int StartLoop() => throw new InvalidOperationException();

        public void EndLoop(int loopId) => throw new InvalidOperationException();

        public void BreakCurrentLoop() => throw new InvalidOperationException();

        public Value GetGraphVariableValue(uint dataIndex) => throw new InvalidOperationException();

        public void SetGraphVariableValue(uint dataIndex, Value value) => throw new InvalidOperationException();
        public OutputDataPort GetPulledDataPort()
        {
            throw new NotImplementedException();
        }

        public bool ReadBool(InputDataPort port) => ReadValue(port, out Value val) ? val.Bool : default;

        public int ReadInt(InputDataPort port) => ReadValue(port, out Value val) ? val.Int : default;

        public float ReadFloat(InputDataPort port) => ReadValue(port, out Value val) ? val.Float : default;

        public Vector2 ReadVector2(InputDataPort port) => ReadValue(port, out Value val) ? val.Float2 : default;

        public Vector3 ReadVector3(InputDataPort port) => ReadValue(port, out Value val) ? val.Float3 : default;

        public Vector4 ReadVector4(InputDataPort port) => ReadValue(port, out Value val) ? val.Float4 : default;

        public Quaternion ReadQuaternion(InputDataPort port) => ReadValue(port, out Value val) ? val.Quaternion : default;
        public Color ReadColor(InputDataPort port) => ReadValue(port, out Value val) ? val.Color : default;


        public T ReadObject<T>(InputDataPort port) where T : class => throw new InvalidOperationException();

        public T ReadStruct<T>(InputDataPort port) where T : struct => throw new InvalidOperationException();

        public Value ReadValue(InputDataPort port) => ReadValue(port, out Value val) ? val : default;
    }
}
