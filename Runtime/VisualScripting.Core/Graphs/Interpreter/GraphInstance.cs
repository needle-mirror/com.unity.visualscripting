#define VS_TRACING
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;


namespace Unity.VisualScripting.Interpreter
{
    internal class GraphInstance : IGraphInstance, IDisposable
    {
        internal const int k_MaxNodesPerFrame = 1024;
        internal const uint k_NoCoroutineId = 0;

        static ProfilerMarker s_ResumeFrameMarker = new ProfilerMarker("Resume Frame");

        static Dictionary<Type, Type> NodeTypeToNodeStateType = new Dictionary<Type, Type>();
        static Dictionary<Type, Type> NodeTypeToCoroutineNodeStateType = new Dictionary<Type, Type>();
        /// <summary>
        /// Node execution entry point
        /// </summary>
        public struct NodeExecution : IEquatable<NodeExecution>
        {
            /// <summary>
            /// triggered node index
            /// </summary>
            public NodeId NodeId;

            /// <summary>
            /// triggered port index
            /// </summary>
            public uint PortIndex;

            public uint CoroutineId;

            public bool Equals(NodeExecution other)
            {
                return NodeId.Equals(other.NodeId) && PortIndex == other.PortIndex && CoroutineId == other.CoroutineId;
            }

            public override bool Equals(object obj)
            {
                return obj is NodeExecution other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = NodeId.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)PortIndex;
                    hashCode = (hashCode * 397) ^ (int)CoroutineId;
                    return hashCode;
                }
            }
        }

#if VS_TRACING
        private DotsFrameTrace FrameTrace { get; set; }
#endif

        public readonly uint Hash;
        readonly GraphDefinition m_Definition;
        readonly Value[] m_DataValues;
        ActiveNodesState m_State;
        Dictionary<int, int> m_ObjectsRefCount = new Dictionary<int, int>();

        struct CoroutineState : IDisposable
        {
            public readonly uint CoroutineId;
            public NativeArray<byte> NodeCoroutineStates;

            public CoroutineState(uint coroutineId, NativeArray<byte> states = default)
            {
                CoroutineId = coroutineId;
                NodeCoroutineStates = states;
            }

            public void Dispose()
            {
                if (NodeCoroutineStates.IsCreated)
                    NodeCoroutineStates.Dispose();
            }
        }

        private int m_NodeCoroutineStatesTotalByteLength;
        private List<CoroutineState> m_CoroutineStates = new List<CoroutineState>();
        private HashSet<uint> m_FinishedCoroutinesToDispose = new HashSet<uint>();
        private uint m_CurrentCoroutineId;
        private uint m_NextCoroutineId = 1;

        /// Each node instance's state
        NativeArray<byte> m_NodeStates;

        /// <summary>
        /// offset of the state struct in m_NodeStates for node with index i
        /// </summary>
        int[] m_NodeStateOffsets;
        int[] m_NodeStateCoroutineOffsets;

        /// Stores the state of the IFlowNode&lt;TState&gt; being executed
        unsafe void* m_CurrentNodeState;
        int m_CurrentNodeStateOffset;
        int m_CurrentNodeCoroutineStateOffset;

        public GraphInstance(GameObject currentGameObject, GraphDefinition definition, GraphInstance parent,
                             NodeId parentNodeId, uint hash = 0) : this(currentGameObject, definition, hash)
        {
            m_Parent = parent;
            m_ParentNodeId = parentNodeId;
        }

        public GraphInstance(GameObject currentGameObject, GraphDefinition definition, uint hash = 0)
        {
            m_Definition = definition;
            Hash = hash;
            CurrentEntity = currentGameObject;

            m_State.Init();
            m_DataValues = new Value[m_Definition.DataPortTable.Length];

            // TODO bake NodeStateOffsets and totalSize in the definition during translation. to reduce the size of unused slots we could sort nodes in the definition itself (nodes with state first, everything else after)
            m_NodeStateOffsets = new int[definition.NodeTable.Length];
            m_NodeStateCoroutineOffsets = new int[definition.NodeTable.Length];
            m_NestedGraphInstances = new GraphInstance[definition.GraphReferences.Length];

            int totalSize = 0;
            for (var i = 0; i < definition.NodeTable.Length; i++)
            {
                if (definition.NodeTable[i] is GraphInputNode graphInputNode)
                    GraphInput = graphInputNode;
                else if (definition.NodeTable[i] is GraphOutputNode graphOutputNode)
                    GraphOutput = graphOutputNode;
                else if (definition.NodeTable[i] is SubGraphNode superUnitNode)
                {
                    var graphReference = definition.GraphReferences[superUnitNode.NestedGraphAssetIndex.Index];
                    m_NestedGraphInstances[superUnitNode.NestedGraphAssetIndex.Index] = new GraphInstance(currentGameObject, graphReference.GraphDefinition, this, new NodeId((uint)i));
                }

                // node state (one instance per graph)
                if (definition.NodeTable[i] is IStatefulNode statefulNode)
                {
                    int stateSize = GetNodeStateSize(statefulNode, NodeTypeToNodeStateType, typeof(IStatefulNode<>));
                    m_NodeStateOffsets[i] = stateSize == 0 ? -1 : totalSize;
                    totalSize += stateSize;
                }
                else
                {
                    m_NodeStateOffsets[i] = -1;
                }

                // coroutine node state (multiple instances, one per coroutine)
                if (definition.NodeTable[i] is ICoroutineStatefulNode statefulCoroutineNode)
                {
                    int stateSize = GetNodeStateSize(statefulCoroutineNode, NodeTypeToCoroutineNodeStateType, typeof(ICoroutineStatefulNode<>));
                    Assert.AreNotEqual(0, stateSize); //
                    m_NodeStateCoroutineOffsets[i] = totalSize;
                    m_NodeCoroutineStatesTotalByteLength += stateSize;
                }
                else
                {
                    m_NodeStateCoroutineOffsets[i] = -1;
                }
            }

            m_NodeStates = new NativeArray<byte>(totalSize, Allocator.Persistent);

            for (var i = 0; i < definition.NodeTable.Length; i++)
            {
                if (!(definition.NodeTable[i] is IStatefulNode statefulNode))
                    continue;

                var nodeStateOffset = m_NodeStateOffsets[i];
                Assert.IsTrue(nodeStateOffset >= 0 || nodeStateOffset == -1);

                unsafe
                {
                    if (nodeStateOffset != -1)
                        m_CurrentNodeState = (byte*)m_NodeStates.GetUnsafePtr() + nodeStateOffset;
                    statefulNode.Init(this);
                    m_CurrentNodeState = null;
                }
            }

            // Execute constants once.
            // TODO: potential optimization: just strip constants from the graph definition and initialize the value array accordingly
            for (var i = 0; i < definition.NodeTable.Length; i++)
            {
                if (definition.NodeTable[i] is IConstantNode constantNode)
                {
                    IDataNode dataNode = constantNode;
                    ExecuteDataNode(new NodeId((uint)i), ref dataNode);
                }
                else if (definition.NodeTable[i] is IEntryPointRegisteredNode registeredNode)
                {
                    var nodeId = new NodeId((uint)i);
                    registeredNode.Register(this, nodeId);
                }
            }

            for (var index = 0; index < definition.VariableInitValues.Count; index++)
            {
                var initValue = definition.VariableInitValues[index];
                m_DataValues[(int)initValue.DataIndex] = initValue.Value;
            }
        }

        public void Dispose()
        {
            if (!m_NodeStates.IsCreated)
                return;
            m_NodeStates.Dispose();
            foreach (var gcHandles in m_DataValues.Where(v => v.Type == ValueType.ManagedObject))
            {
                if (gcHandles.Handle.IsAllocated)
                    gcHandles.Handle.Free();
            }

            foreach (var coroutineState in m_CoroutineStates)
            {
                coroutineState.Dispose();
            }

            foreach (var nestedGraphInstance in m_NestedGraphInstances)
            {
                nestedGraphInstance?.Dispose();
            }

            foreach (var registeredDelegate in _registeredDelegates)
            {
                EventBus.Unregister(registeredDelegate.Item1, registeredDelegate.Item2);
            }
        }

        internal static int GetNodeStateSize(IFlowNode baseFlowNode, Dictionary<Type, Type> mapping, Type statefulNodeType)
        {
            var nodeType = baseFlowNode.GetType();
            if (!mapping.TryGetValue(nodeType, out var t))
            {
                var iFlowNodeInterface = nodeType.GetInterfaces().SingleOrDefault(i =>
                    i.IsConstructedGenericType && i.GetGenericTypeDefinition() == statefulNodeType);
                t = iFlowNodeInterface == null ? null : iFlowNodeInterface.GetGenericArguments()[0];
                mapping.Add(nodeType, t);
            }

            var stateType = t;

            return stateType == null ? 0 : UnsafeUtility.SizeOf(stateType);
        }

        /// <summary>
        /// Run a node, either from a trigger port, or an EntryPoint
        /// </summary>
        /// <param name="exec">Description of the triggered node and port</param>
        /// <param name="evt">The event that is being processed</param>
        /// <returns>status of execution, whether the task is done or ongoing</returns>
        /// <exception cref="InvalidDataException">Thrown if unable to run the node</exception>
        private Execution ExecuteNode(NodeExecution exec)
        {
            m_CurrentCoroutineId = exec.CoroutineId;
            Assert.IsTrue(exec.NodeId.IsValid());
            NodeId nodeId = exec.NodeId;
            var node = m_Definition.NodeTable[(int)nodeId.GetIndex()];

            // if (ScriptingGraphRuntime != null)
            // m_EventValuesCountBeforeLastNodeExecution = ScriptingGraphRuntime.GetEventValues().Length;
            Execution execution = Execution.Done;
#if VS_TRACING
            try
            {
#endif
                TriggeredByCurrentNode.Clear();
                execution = ExecuteNode(exec, node, nodeId);
                for (var i = TriggeredByCurrentNode.Count - 1; i >= 0; i--)
                {
                    var e = TriggeredByCurrentNode[i];
                    m_State.AddExecutionThisFrame(e);
                }
#if VS_TRACING
            }

            catch (Exception e)
            {
                if (FrameTrace != null)
                    FrameTrace.RecordError(exec.NodeId, e);
                else
                    throw;
            }

            if (execution == Execution.Running)
                if (!(node is IUpdatableNode))
                    Assert.IsTrue(false,
                        $"The node type '{node.GetType().Name}' returned {nameof(Execution.Running)}, but does not implement {nameof(IUpdatableNode)}");

            byte progress = Byte.MinValue;
            if (node is INodeReportProgress reportProgress)
            {
                progress = reportProgress.GetProgress(this);
            }

            FrameTrace?.RecordExecutedNode(exec.NodeId, progress);
#endif

            // Do that AFTER the recording, which calls GetProgress, which relies on the state
            unsafe
            {
                m_CurrentNodeState = null;
            }
            return execution;
        }

        private Execution ExecuteNode(NodeExecution exec, INode node, NodeId nodeId)
        {
            var inputPort = new InputTriggerPort { Port = new Port { Index = exec.PortIndex } };
            // Debug.Log(m_CurrentCoroutineId);
            switch (node)
            {
                case IStatefulNode stateFlowNode:
                    {
                        m_CurrentNodeStateOffset = m_NodeStateOffsets[nodeId.GetIndex()];
                        Assert.IsTrue(m_CurrentNodeStateOffset >= 0 || m_CurrentNodeStateOffset == -1);
                        unsafe
                        {
                            m_CurrentNodeState = m_CurrentNodeStateOffset == -1
                                ? null
                                : (byte*)m_NodeStates.GetUnsafePtr() + m_CurrentNodeStateOffset;
                        }

                        Execution execution;
                        if (exec.PortIndex == 0)
                        {
                            Assert.IsTrue(stateFlowNode is IUpdatableNode || stateFlowNode is IEntryPointNode,
                                $"Stateful nodes must either implement {nameof(IUpdatableNode)} or {nameof(IEntryPointNode)}");
                            execution = stateFlowNode is IUpdatableNode updatableNode
                                ? updatableNode.Update(this)
                                : ((IEntryPointNode)stateFlowNode).Execute(this);
                        }
                        else
                            execution = ((IFlowNode)stateFlowNode).Execute(this, inputPort);

                        return execution;
                    }
                case IUpdatableNode updatableNode:
                    {
                        Execution execution;
                        if (exec.PortIndex == 0)
                            execution = updatableNode.Update(this);
                        else
                            execution = updatableNode.Execute(this, inputPort);

                        return execution;
                    }
                case IFlowNode flowNode:
                    return flowNode.Execute(this, inputPort);
                case IEntryPointNode entryPointNode:
                    return entryPointNode.Execute(this);
                // this is AFTER the IFlowNode case, as SubGraphNode implements both
                case IDataNode _:
                    Assert.IsTrue(false);
                    break;
            }

            throw new InvalidDataException();
        }

        private void ExecuteDataNode(NodeId nodeId, ref IDataNode dataNode)
        {
#if VS_TRACING
            try
            {
                dataNode.Execute(this);
            }
            catch (Exception e)
            {
                if (FrameTrace != null)
                    FrameTrace.RecordError(nodeId, e);
                else
                    throw;
            }
#else

            dataNode.Execute(this);
#endif
        }

        static void AssertPortIsValid(Port p)
        {
            Assert.AreNotEqual(0u, p.Index, "Port has not been initialized: its index is 0");
        }

        public bool ReadValueInOutputPort(OutputDataPort port, out Value val)
        {
            AssertPortIsValid(port.GetPort());
            var portInfo = m_Definition.PortInfoTable[port.Port.Index];
            if (!portInfo.IsDataPort)
                Assert.IsTrue(false,
                    $"Trying to read a value from a trigger port: {portInfo.PortName}:{port.Port.Index} in node {portInfo.NodeId}:{m_Definition.NodeTable[(int)portInfo.NodeId.GetIndex()]}");
            uint dataSlot = portInfo.DataOrTriggerIndex;
            val = ReadValueInDataSlot(dataSlot);
            return val.Type != ValueType.Unknown;
        }

        private bool ReadValue(InputDataPort port, out Value value, ValueType coerceToType = ValueType.Unknown)
        {
            // An intput data node has at most one incoming edge
            // The output & input data port linked by an edge will point to the same Data index
            AssertPortIsValid(port.GetPort());
            Assert.IsTrue(port.GetPort().Index < m_Definition.PortInfoTable.Length);

            int portIndex = (int)port.GetPort().Index;
            var portInfo = m_Definition.PortInfoTable[portIndex];
            if (!portInfo.IsDataPort)
                Assert.IsTrue(false,
                    $"Trying to read a value from a trigger port: {portInfo.PortName}:{port.Port.Index} in node {portInfo.NodeId}:{m_Definition.NodeTable[(int)portInfo.NodeId.GetIndex()]}");
            uint dataSlot = portInfo.DataOrTriggerIndex;

            if (dataSlot != 0)
            {
                var pulledPortIndex = m_Definition.DataPortTable[(int)dataSlot];
                var dependencyNodeId = m_Definition.PortInfoTable[pulledPortIndex].NodeId;

                // Only pull data nodes
                // Disconnected ports have 0 (NULL) as daat Slots
                // Do not pull on constant nodes, since they never change
                if (
                    dependencyNodeId
                        .IsValid() && // if the port is connected to a variable, it will have a data slot but no dependent node, as the "get variable" does not generate a runtime node
                    m_Definition.NodeTable[(int)dependencyNodeId.GetIndex()] is IDataNode dataNode
                    && !(dataNode is IConstantNode))
                {
#if VS_TRACING
                    FrameTrace?.RecordExecutedNode(dependencyNodeId, 0);
#endif
                    _pulledPort = new OutputDataPort { Port = { Index = (uint)pulledPortIndex } };
                    ExecuteDataNode(dependencyNodeId, ref dataNode);
                    _pulledPort = default;
                }

                var val = m_DataValues[(int)dataSlot];

                if (val.Type != ValueType.Unknown)
                {
                    value = Value.CoerceValueToType(coerceToType, val);
#if VS_TRACING
                    FrameTrace?.RecordReadValue(value, port);
#endif
                    return true;
                }
            }

            value = default;
#if VS_TRACING
            FrameTrace?.RecordReadValue(value, port);
#endif
            return false;
        }

        /// <summary>
        /// Trigger every entry point of a specific type4
        /// </summary>
        /// <typeparam name="T">Type of entry points to trigger</typeparam>
        public void TriggerEntryPoints<T>() where T : struct, IEntryPointNode
        {
            for (int i = 0; i < m_Definition.NodeTable.Length; i++)
            {
                var index = i;
                var n = m_Definition.NodeTable[i];
                if (n is T)
                {
                    m_State.AddExecutionThisFrame(new NodeId((uint)index));
                }
            }
        }

        public void FinishFrame()
        {
            m_State.SwapNextFrameQueues();
            for (var index = 0; index < m_State.CoroutineNodesToProcessThisFrame.Count; index++)
            {
                var nodeExecution = m_State.CoroutineNodesToProcessThisFrame[index];
                m_FinishedCoroutinesToDispose.Remove(nodeExecution.CoroutineId);
            }
        }

        public enum ResumeFramePhase
        {
            None,
            Standard,
            Coroutine,
            CoroutineEndOfFrame,
        }

        /// <summary>
        /// Runs nodes in the <see cref="ActiveNodesState.NodesToExecute"/> list and add them to the <see cref="ActiveNodesState.NextFrameNodes"/> list if needed
        /// </summary>
        public void ResumeFrame(ResumeFramePhase phase)
        {
            switch (phase)
            {
                case ResumeFramePhase.Coroutine:
                    {
                        m_State.QueueCoroutineNodes();
                        GarbageCollectFinishedCoroutineStates();

                        break;
                    }
                case ResumeFramePhase.CoroutineEndOfFrame:
                    m_State.QueueEndOfFrameNodes();
                    break;
            }

            if (phase == ResumeFramePhase.Coroutine)
            {
            }


#if VS_TRACING
            if ( /*ScriptingGraphRuntimeInit.s_TracingEnabled &&*/ FrameTrace == null)
            {
                FrameTrace = new DotsFrameTrace();
                FrameTrace.entity = CurrentEntity;
                FrameTrace.hash = Hash;
                FrameTrace.frameCount = Time.frameCount;
            }
#endif
            m_TimeData.DeltaTime = Time.deltaTime;
            m_TimeData.UnscaledDeltaTime = Time.unscaledDeltaTime;
#if UNITY_INCLUDE_TESTS
            if (m_OverrideTimeData != null)
                m_TimeData = m_OverrideTimeData.Invoke();
#endif
            s_ResumeFrameMarker.Begin();
            // m_OutputTriggersActivated = mOutputTriggersPerEntityGraphActivated;
            // Time = time;

            int nodeExecuted = 0;
            // TODO nodes to execute = NodeToExecute or ThisFrameCoroutineQueue
            while ((m_State.NodesToExecuteCount > 0 || m_State.EnqueueNodesToExecuteLater()))
            {
                // Check for endless cycle & stop when k_MaxNodesPerFrame are exectued
                if (nodeExecuted++ >= k_MaxNodesPerFrame)
                {
                    Debug.LogWarning(
                        $"Trying to execute more than {k_MaxNodesPerFrame} nodes in a frame, something seems wrong.");
                    break;
                }

                // Pop top node & Execute it
                var activeExec = m_State.NodesToExecutePop();

                var exec = ExecuteNode(activeExec);
                switch (exec)
                {
                    // If the node needs to execute & wait for a while, we reschedule it for next frame
                    // else, If the current call stop/disable the node, ensure it is NOT executed next frame
                    case Execution.Running:
                        {
                            m_State.NextFrameNodesAdd(activeExec.NodeId, m_CurrentCoroutineId);
                            m_FinishedCoroutinesToDispose.Remove(m_CurrentCoroutineId);
                            break;
                        }
                    case Execution.Yield:
                        {
                            m_State.AddExecutionLaterThisFrame(activeExec.NodeId, m_CurrentCoroutineId);
                            break;
                        }
                    case Execution.YieldUntilEndOfFrame:
                        {
                            m_State.AddExecutionAtTheEndOfFrame(activeExec);
                            m_FinishedCoroutinesToDispose.Remove(m_CurrentCoroutineId);
                            break;
                        }
                }
            }

            // Debug.Log($"cor {m_CurrentCoroutineId}");
            s_ResumeFrameMarker.End();
        }

        private void GarbageCollectFinishedCoroutineStates()
        {
            foreach (var id in m_FinishedCoroutinesToDispose)
            {
                var i = FindCoroutineStateIndex(id);

                Debug.Log("Dispose coroutine " + id);
                m_CoroutineStates[i].Dispose();
                m_CoroutineStates.RemoveAt(i);
            }

            m_FinishedCoroutinesToDispose.Clear();
            m_FinishedCoroutinesToDispose.AddRange(m_CoroutineStates.Select(c => c.CoroutineId));
        }

        private int FindCoroutineStateIndex(uint id)
        {
            for (var index = 0; index < m_CoroutineStates.Count; index++)
            {
                var coroutineState = m_CoroutineStates[index];
                if (coroutineState.CoroutineId == id)
                    return index;
            }

            return -1;
        }

        internal void WriteValueToDataSlot(uint dataSlot, Value value)
        {
            // TODO disconnected ports should not have a port index in the first place
            if (dataSlot == 0)
                return;
            var slotIndex = (int)dataSlot;
            var prevValue = m_DataValues[slotIndex];

            if (prevValue.Type == ValueType.ManagedObject || prevValue.Type == ValueType.Struct)
            {
                var handle = prevValue.Handle.GetHashCode();
                if (m_ObjectsRefCount.TryGetValue(handle, out var refCount))
                {
                    m_ObjectsRefCount[handle] = --refCount;

                    if (refCount <= 0)
                    {
                        prevValue.Handle.Free();
                        m_ObjectsRefCount.Remove(handle);
                    }
                }
            }

            if ((value.Type == ValueType.ManagedObject || prevValue.Type == ValueType.Struct) && value.Handle.Target != null)
            {
                var handle = value.Handle.GetHashCode();
                if (!m_ObjectsRefCount.TryGetValue(handle, out var refCount))
                {
                    m_ObjectsRefCount.Add(handle, 0);
                }

                m_ObjectsRefCount[handle] = ++refCount;
            }

            m_DataValues[slotIndex] = value;
        }

        internal Value ReadValueInDataSlot(uint dataSlot) => m_DataValues[(int)dataSlot];

        public GameObject CurrentEntity { get; set; }
        private TimeData m_TimeData;

#if UNITY_INCLUDE_TESTS
        internal Func<TimeData> m_OverrideTimeData;
#endif
        public TimeData TimeData
        {
            get => m_TimeData;
            internal set => m_TimeData = value;
        }

        public unsafe ref TS GetState<T, TS>(in T _) where T : IStatefulNode<TS> where TS : unmanaged, INodeState
        {
            Assert.IsFalse(m_CurrentNodeState == null);
            Assert.IsFalse(UnsafeUtility.SizeOf<TS>() == 0);
            return ref UnsafeUtility.AsRef<TS>(m_CurrentNodeState);
        }

        public unsafe ref TS GetCoroutineState<T, TS>(in T _) where T : ICoroutineStatefulNode<TS> where TS : unmanaged, INodePerCoroutineState
        {
            Assert.IsFalse(m_CurrentCoroutineId == 0);

            // lazy-initialize the coroutine node states copy here
            int coroutineIndex = FindCoroutineStateIndex(m_CurrentCoroutineId);
            CoroutineState state;
            if (coroutineIndex == -1)
            {
                // Debug.Log("Allocate coroutine " + m_CurrentCoroutineId);
                state = new CoroutineState(m_CurrentCoroutineId,
                    new NativeArray<byte>(m_NodeCoroutineStatesTotalByteLength, Allocator.Persistent));
                m_CoroutineStates.Add(state);
            }
            else
                state = m_CoroutineStates[coroutineIndex];

            // just hijack the pointer to this precise node state from the coroutine array
            var currentNodeCoroutineState =
                (byte*)state.NodeCoroutineStates.GetUnsafePtr() + m_CurrentNodeCoroutineStateOffset;

            Assert.IsFalse(UnsafeUtility.SizeOf<TS>() == 0);
            return ref UnsafeUtility.AsRef<TS>(currentNodeCoroutineState);
        }

        private HashSet<(EventHook, Delegate)> _registeredDelegates = new HashSet<(EventHook, Delegate)>();
        public void RegisterEventHandler<T>(NodeId id, IEntryPointRegisteredNode<T> node, string hookName,
            InputDataPort targetGameObjectPort)
        {
            // TODO document and make clearer, maybe codegen the right thing
            Object target = targetGameObjectPort != default ? (this.ReadObject<GameObject>(targetGameObjectPort) ?? CurrentEntity) : null;
            target = target ? target : CurrentEntity.GetComponent<IMachine>() as Object;

            if (node.MessageListenerType != null)
                MessageListener.AddTo(node.MessageListenerType, target is GameObject targetGo ? targetGo : target is Component c ? c.gameObject : CurrentEntity);
            Action<T> handler = (T args) =>
            {
                m_State.AddExecutionThisFrame(id);
                node.AssignArguments(this, args);
                ResumeFrame(GraphInstance.ResumeFramePhase.Standard);
            };
            var eventHook = new EventHook(hookName, target ? target : CurrentEntity);
            _registeredDelegates.Add((eventHook, handler));
            EventBus.Register(eventHook, handler);
        }

        public ReflectedMember GetReflectedMember(uint memberIndex)
        {
            return m_Definition.ReflectedMembers[(int)memberIndex];
        }

        private List<NodeExecution> TriggeredByCurrentNode = new List<NodeExecution>();
        private OutputDataPort _pulledPort;
        private GraphInstance[] m_NestedGraphInstances;
        private GraphInstance m_Parent;
        private NodeId m_ParentNodeId;
        private GraphInputNode GraphInput;
        private GraphOutputNode GraphOutput;

        // TODO why
        public void TriggerImmediately(OutputTriggerPort output)
        {
#if VS_TRACING
            FrameTrace?.RecordTriggeredPort(output);
#endif

            int portIndex = (int)output.Port.Index;
            Assert.IsTrue(portIndex < m_Definition.PortInfoTable.Length);
            uint triggerIndex = m_Definition.PortInfoTable[portIndex].DataOrTriggerIndex;

            // disconnected
            if (triggerIndex == 0)
                return;

            Assert.IsTrue(triggerIndex < m_Definition.PortInfoTable.Length);
            m_State.AddExecutionThisFrame(new NodeExecution
            {
                NodeId = m_Definition.PortInfoTable[(int)triggerIndex].NodeId,
                PortIndex = triggerIndex,
                CoroutineId = m_CurrentCoroutineId,
            });
        }

        public void Trigger(OutputTriggerPort output, bool asCoroutine = false)
        {
#if VS_TRACING
            FrameTrace?.RecordTriggeredPort(output);
#endif

            int portIndex = (int)output.Port.Index;
            Assert.IsTrue(portIndex < m_Definition.PortInfoTable.Length);
            uint triggerIndex = m_Definition.PortInfoTable[portIndex].DataOrTriggerIndex;

            // disconnected
            if (triggerIndex == 0)
                return;

            Assert.IsTrue(triggerIndex < m_Definition.PortInfoTable.Length);
            TriggeredByCurrentNode.Add(new NodeExecution
            {
                NodeId = m_Definition.PortInfoTable[(int)triggerIndex].NodeId,
                PortIndex = triggerIndex,
                CoroutineId = asCoroutine ? GetNextCoroutineId() : m_CurrentCoroutineId,
            });

            m_FinishedCoroutinesToDispose.Remove(m_CurrentCoroutineId);

            uint GetNextCoroutineId()
            {
                var nextCoroutineId = m_NextCoroutineId;
                m_NextCoroutineId = m_NextCoroutineId == UInt32.MaxValue ? 1 : m_NextCoroutineId + 1;
                return nextCoroutineId;
            }
        }

        public void Write(OutputDataPort port, Value value)
        {
            AssertPortIsValid(port.GetPort());
            Assert.IsTrue(port.GetPort().Index < m_Definition.PortInfoTable.Length);
            int portIndex = (int)port.GetPort().Index;
            uint dataSlot = m_Definition.PortInfoTable[portIndex].DataOrTriggerIndex;
#if VS_TRACING
            FrameTrace?.RecordWrittenValue(value, port);
#endif

            WriteValueToDataSlot(dataSlot, value);
        }

        public int CurrentLoopId { get; private set; }
        public int StartLoop()
        {
            return ++CurrentLoopId;
        }

        public void EndLoop(int loopId)
        {
            Assert.AreEqual(loopId, CurrentLoopId);
            CurrentLoopId--;
        }

        public void BreakCurrentLoop()
        {
            Assert.AreNotEqual(0, CurrentLoopId);
            CurrentLoopId--;
        }

        public Value GetGraphVariableValue(uint dataIndex)
        {
            return m_DataValues[dataIndex];
        }

        public void SetGraphVariableValue(uint dataIndex, Value value)
        {
            m_DataValues[dataIndex] = value;
        }

        public OutputDataPort GetPulledDataPort()
        {
            AssertPortIsValid(_pulledPort.Port);
            return _pulledPort;
        }

        public void TriggerNestedGraphInput(ScriptGraphAssetIndex nestedGraphIndex,
            uint inputIndex)
        {
            GraphInputNode input = m_NestedGraphInstances[nestedGraphIndex.Index].GraphInput;

            m_NestedGraphInstances[nestedGraphIndex.Index].TriggerImmediately(input.Triggers.SelectPort(inputIndex));
            m_NestedGraphInstances[nestedGraphIndex.Index].ResumeFrame(ResumeFramePhase.Standard);
        }

        public void TriggerParentGraphOutput(uint outputIndex)
        {
            var superUnitNode = GetParentSuperUnitNode();
            m_Parent.TriggerImmediately(superUnitNode.OutputTriggers.SelectPort(outputIndex));
        }

        public Value PullNestedGraphDataOutput(ScriptGraphAssetIndex nestedGraphIndex, uint inputIndex)
        {
            GraphOutputNode output = m_NestedGraphInstances[nestedGraphIndex.Index].GraphOutput;
            return m_NestedGraphInstances[nestedGraphIndex.Index].ReadValue(output.Datas.SelectPort(inputIndex));
        }

        public Value PullParentGraphDataInput(uint inputIndex)
        {
            var superUnitNode = GetParentSuperUnitNode();
            return m_Parent.ReadValue(superUnitNode.InputDatas.SelectPort(inputIndex));
        }

        private SubGraphNode GetParentSuperUnitNode()
        {
            var node = m_Parent.m_Definition.NodeTable[m_ParentNodeId.GetIndex()];
            if (!(node is SubGraphNode superUnitNode))
                throw new NotImplementedException();
            return superUnitNode;
        }

        public bool ReadBool(InputDataPort port) => ReadValue(port, out Value val) ? val.Bool : default;

        public int ReadInt(InputDataPort port) => ReadValue(port, out Value val) ? val.Int : default;

        public float ReadFloat(InputDataPort port) => ReadValue(port, out Value val) ? val.Float : default;

        public Vector2 ReadVector2(InputDataPort port) => ReadValue(port, out Value val) ? val.Float2 : default;

        public Vector3 ReadVector3(InputDataPort port) => ReadValue(port, out Value val) ? val.Float3 : default;

        public Vector4 ReadVector4(InputDataPort port) => ReadValue(port, out Value val) ? val.Float4 : default;

        public Quaternion ReadQuaternion(InputDataPort port) => ReadValue(port, out Value val) ? val.Quaternion : default;
        public Color ReadColor(InputDataPort port) => ReadValue(port, out Value val) ? val.Color : default;

        public T ReadObject<T>(InputDataPort port) where T : class => ReadValue(port, out Value val) ? val.Object<T>() : default(T);
        // TODO: get rid of boxing
        public T ReadStruct<T>(InputDataPort port) where T : struct => (T)(ReadValue(port, out Value val) ? val.Object(typeof(T)) : default(T));

        public Value ReadValue(InputDataPort port) => ReadValue(port, out Value val) ? val : default;

        public IEnumerator InterpreterCoroutine()
        {
            while (true)
            {
                // yield null/next frame, waitForSeconds, until, ...
                yield return null;
                ResumeFrame(ResumeFramePhase.Coroutine);
                // end of frame
                yield return new WaitForEndOfFrame();
                ResumeFrame(ResumeFramePhase.CoroutineEndOfFrame);

                // swap queues
                FinishFrame();
            }
        }
    }

    internal struct ActiveNodesState
    {
        /// <summary>
        /// List of nodes to execute this frame
        /// </summary>
        private Stack<GraphInstance.NodeExecution> NodesToExecute;

        private List<GraphInstance.NodeExecution> NodesToExecuteLaterThisFrame;

        // yield return new WaitForEndOfFrame() equivalent
        private List<GraphInstance.NodeExecution> NodesToExecuteAtTheEndOfFrame;

        /// <summary>
        /// List of nodes to execute next frame
        /// </summary>
        private List<GraphInstance.NodeExecution> NextFrameNodesA, NextFrameNodesB;

        private bool flipped;

        internal List<GraphInstance.NodeExecution> CoroutineNodesToProcessThisFrame =>
            flipped ? NextFrameNodesA : NextFrameNodesB;
        private List<GraphInstance.NodeExecution> NextFrameCoroutineNodeQueue =>
            flipped ? NextFrameNodesB : NextFrameNodesA;
        // private List<GraphInstance.NodeExecution> NextFrameNodesFromCoroutine;

        public int NodesToExecuteCount => NodesToExecute.Count;

        public void Init()
        {
            Assert.IsNull(NodesToExecute);
            // Assert.IsNull(NextFrameNodesFromCoroutine);
            NodesToExecute = new Stack<GraphInstance.NodeExecution>(GraphInstance.k_MaxNodesPerFrame);
            // NextFrameNodesFromCoroutine = new List<GraphInstance.NodeExecution>();
            // NextFrameNodes = new List<GraphInstance.NodeExecution>();
            NodesToExecuteLaterThisFrame = new List<GraphInstance.NodeExecution>();
            NextFrameNodesA = new List<GraphInstance.NodeExecution>();
            NextFrameNodesB = new List<GraphInstance.NodeExecution>();
            NodesToExecuteAtTheEndOfFrame = new List<GraphInstance.NodeExecution>();
        }

        /// <summary>
        /// Plan an execution this frame
        /// </summary>
        /// <param name="nodeId">Node to trigger</param>
        /// <param name="portIndex">Trigger port</param>
        /// <param name="coroutine">New coroutine id</param>
        public void AddExecutionThisFrame(NodeId nodeId, uint portIndex = 0, uint coroutine = 0)
        {
            AddExecutionThisFrame(new GraphInstance.NodeExecution { NodeId = nodeId, PortIndex = portIndex, CoroutineId = coroutine });
        }

        public void AddExecutionThisFrame(GraphInstance.NodeExecution exec)
        {
            Assert.IsTrue(exec.NodeId.IsValid());
            if (NodesToExecute == null)
                Init();
            NodesToExecute.Push(exec);
        }

        public void AddExecutionAtTheEndOfFrame(GraphInstance.NodeExecution exec)
        {
            Assert.IsTrue(exec.NodeId.IsValid());
            if (NodesToExecute == null)
                Init();
            exec.PortIndex = 0;
            NodesToExecuteAtTheEndOfFrame.Add(exec);
        }

        public void QueueEndOfFrameNodes()
        {
            for (int i = 0; i < NodesToExecuteAtTheEndOfFrame.Count; i++)
                NodesToExecute.Push(NodesToExecuteAtTheEndOfFrame[i]);
            NodesToExecuteAtTheEndOfFrame.Clear();
        }

        public void AddExecutionLaterThisFrame(NodeId nodeId, uint coroutineId = 0)
        {
            Assert.IsTrue(nodeId.IsValid());
            if (NodesToExecute == null)
                Init();
            NodesToExecuteLaterThisFrame.Add(new GraphInstance.NodeExecution { NodeId = nodeId, CoroutineId = coroutineId });
        }

        public bool EnqueueNodesToExecuteLater()
        {
            if (NodesToExecuteLaterThisFrame.Count == 0)
                return false;
            for (int i = 0; i < NodesToExecuteLaterThisFrame.Count; i++)
            {
                NodesToExecute.Push(NodesToExecuteLaterThisFrame[i]);
            }

            NodesToExecuteLaterThisFrame.Clear();
            return true;
        }

        public void ClearNodesToExecute()
        {
            NodesToExecute.Clear();
        }

        public GraphInstance.NodeExecution NodesToExecutePop()
        {
            return NodesToExecute.Pop();
        }

        public void NextFrameNodesAdd(NodeId nodeId, uint coroutineId = 0)
        {
            var nodeExecution = new GraphInstance.NodeExecution { NodeId = nodeId, PortIndex = 0, CoroutineId = coroutineId };
            NextFrameCoroutineNodeQueue.Add(nodeExecution);
        }

        public void SwapNextFrameQueues()
        {
            flipped = !flipped;
        }

        public void QueueCoroutineNodes()
        {
            foreach (var nodeExecution in CoroutineNodesToProcessThisFrame)
                AddExecutionThisFrame(nodeExecution);
            CoroutineNodesToProcessThisFrame.Clear();
        }
    }
}
