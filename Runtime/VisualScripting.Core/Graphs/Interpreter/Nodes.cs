using System;
using UnityEngine.UI;

namespace Unity.VisualScripting.Interpreter
{
    // For stateful nodes, the state must implement this interface
    public interface INodeState
    {
    }
    public interface INodePerCoroutineState
    {
    }

    public interface INode
    {
    }
    public interface IFoldableNode : INode { }
    public interface IDataNode : INode
    {
        void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance;
    }


    public interface IConstantNode : IDataNode, IFoldableNode
    {
    }

    public interface IEntryPointNode : INode
    {
        Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance;
    }

    public interface IEntryPointRegisteredNode : IEntryPointNode
    {
        /// <summary>
        /// called to register the event, ATM using Bolt's event bus
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="nodeId"></param>
        /// <typeparam name="TCtx"></typeparam>
        void Register<TCtx>(TCtx ctx, NodeId nodeId) where TCtx : IGraphInstance;
        /// <summary>
        /// If needed, the monobehaviour type to add to the target gameobject to catch events
        /// </summary>
        Type MessageListenerType { get; }
    }

    /// <summary>
    /// Entry point with arguments, requiring argument assignment to the node ports
    /// </summary>
    /// <typeparam name="T">Argument types, eg. EmptyEventArgs</typeparam>
    public interface IEntryPointRegisteredNode<T> : IEntryPointRegisteredNode
    {
        void AssignArguments<TCtx>(TCtx ctx, T args) where TCtx : IGraphInstance;
    }

    public interface IReflectedMemberNode
    {
        uint ReflectedMemberIndex { get; }
    }

    /// <summary>
    /// Flow node with execution ports
    /// </summary>
    public interface IFlowNode : INode
    {
        Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance;
    }

    // Flow node that might require further execution ("update")
    public interface IUpdatableNode : IFlowNode
    {
        Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance;
    }

    // For debugging: report progress of nodes taking multiple execution. not used yet here
    public interface INodeReportProgress : IUpdatableNode
    {
        byte GetProgress<TCtx>(TCtx ctx) where TCtx : IGraphInstance;
    }

    // Non-generic marker for stateful nodes
    public interface IStatefulNode : IFlowNode
    {
        void Init<TCtx>(TCtx ctx) where TCtx : IGraphInstance;
    }
    public interface ICoroutineStatefulNode : IFlowNode
    {
    }

    // ReSharper disable once UnusedTypeParameter
    // Node with a state struct
    public interface IStatefulNode<T> : IStatefulNode where T : struct, INodeState { }
    public interface ICoroutineStatefulNode<T> : ICoroutineStatefulNode where T : struct, INodePerCoroutineState { }
}
