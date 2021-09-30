using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(CountItems))]
    public struct CountItemsNode : IDataNode
    {
        public InputDataPort Collection;

        public OutputDataPort Count;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var enumerable = ctx.ReadObject<IEnumerable>(Collection);
            ctx.Write(Count, enumerable is ICollection collection ? collection.Count : enumerable.Cast<object>().Count());
        }
    }

    [NodeDescription(typeof(FirstItem))]
    public struct FirstItemNode : IDataNode
    {
        public InputDataPort Collection;

        public OutputDataPort FirstItem;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var enumerable = ctx.ReadObject<IEnumerable>(Collection);
            if (enumerable is IList)
                ctx.Write(FirstItem, Value.FromObject(((IList)enumerable)[0]));
            else
                ctx.Write(FirstItem, Value.FromObject(enumerable.Cast<object>().First()));
        }
    }

    [NodeDescription(typeof(LastItem))]
    public struct LastItemNode : IDataNode
    {
        public InputDataPort Collection;

        public OutputDataPort LastItem;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var enumerable = ctx.ReadObject<IEnumerable>(Collection);
            if (enumerable is IList list)
                ctx.Write(LastItem, Value.FromObject(list[list.Count - 1]));
            else
                ctx.Write(LastItem, Value.FromObject(enumerable.Cast<object>().Last()));
        }
    }

    [NodeDescription(typeof(ClearList))]
    public struct ClearListNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public InputDataPort ListInput;
        public OutputDataPort ListOutput;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(ListInput);

            if (list is Array)
            {
                var array = Array.CreateInstance(list.GetType().GetElementType(), 0);
                ctx.Write(ListOutput, Value.FromObject(array));
            }
            else
            {
                list.Clear();
                ctx.Write(ListOutput, Value.FromObject(list));
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(GetListItem))]
    public struct GetListItemNode : IDataNode
    {
        public InputDataPort List;
        public InputDataPort Index;
        public OutputDataPort Item;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(List);
            var index = ctx.ReadInt(Index);
            ctx.Write(Item, Value.FromObject(list[index]));
        }
    }

    [NodeDescription(typeof(RemoveListItemAt))]
    public struct RemoveListItemAtNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public OutputTriggerPort Exit;
        public InputDataPort ListInput;
        public InputDataPort Index;
        public OutputDataPort ListOutput;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(ListInput);
            var index = ctx.ReadInt(Index);

            if (list is Array)
            {
                var resizableList = new ArrayList(list);
                resizableList.RemoveAt(index);
                ctx.Write(ListOutput, Value.FromObject(resizableList));
            }
            else
            {
                list.RemoveAt(index);
                ctx.Write(ListOutput, Value.FromObject(list));
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(CreateList))]
    public struct CreateListNode : IDataNode
    {
        [PortDescription("multiInput")]
        public InputDataMultiPort Elements;

        public OutputDataPort List;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var list = new AotList();
            for (uint i = 0; i < Elements.DataCount; i++)
            {
                list.Add(ctx.ReadObject<object>(Elements.SelectPort(i)));
            }
            ctx.Write(List, Value.FromObject(list));
        }
    }

    [NodeDescription(typeof(AddListItem))]
    public struct AddListItemNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort ListInput;
        public OutputDataPort ListOutput;
        public InputDataPort Item;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(ListInput);
            var item = ctx.ReadObject<object>(Item);

            if (list is Array)
            {
                var resizableList = new ArrayList(list) { item };
                ctx.Write(ListOutput, Value.FromObject(resizableList.ToArray(list.GetType().GetElementType())));
            }
            else
            {
                list.Add(item);
                ctx.Write(ListOutput, Value.FromObject(list));
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(InsertListItem))]
    public struct InsertListItemNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort ListInput;
        public OutputDataPort ListOutput;
        public InputDataPort Index;
        public InputDataPort Item;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(ListInput);
            var index = ctx.ReadInt(Index);
            var item = ctx.ReadObject<object>(Item);

            if (list is Array)
            {
                var resizableList = new ArrayList(list);
                resizableList.Insert(index, item);
                ctx.Write(ListOutput, Value.FromObject(resizableList.ToArray(list.GetType().GetElementType())));
            }
            else
            {
                list.Insert(index, item);
                ctx.Write(ListOutput, Value.FromObject(list));
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(ListContainsItem))]
    public struct ListContainsItemNode : IDataNode
    {
        public InputDataPort List;
        public InputDataPort Item;
        public OutputDataPort Contains;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(List);
            var item = ctx.ReadObject<object>(Item);
            ctx.Write(Contains, list.Contains(item));
        }
    }

    [NodeDescription(typeof(MergeLists))]
    public struct MergeListsNode : IDataNode
    {
        [PortDescription("multiInput")]
        public InputDataMultiPort Elements;

        public OutputDataPort List;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var list = new AotList();

            for (uint i = 0; i < Elements.DataCount; i++)
            {
                list.AddRange(ctx.ReadObject<IEnumerable>(Elements.SelectPort(i)));
            }

            ctx.Write(List, Value.FromObject(list));
        }
    }

    [NodeDescription(typeof(RemoveListItem))]
    public struct RemoveListItemNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort ListInput;
        public OutputDataPort ListOutput;
        public InputDataPort Item;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(ListInput);
            var item = ctx.ReadObject<object>(Item);

            if (list is Array)
            {
                var resizableList = new ArrayList(list);
                resizableList.Remove(item);
                ctx.Write(ListOutput, Value.FromObject(resizableList.ToArray(list.GetType().GetElementType())));
            }
            else
            {
                list.Remove(item);
                ctx.Write(ListOutput, Value.FromObject(list));
            }

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }

    [NodeDescription(typeof(SetListItem))]
    public struct SetListItemNode : IFlowNode
    {
        public InputTriggerPort Enter;
        public InputDataPort List;
        public InputDataPort Index;
        public InputDataPort Item;
        public OutputTriggerPort Exit;

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var list = ctx.ReadObject<IList>(List);
            var index = ctx.ReadInt(Index);
            var item = ctx.ReadObject<object>(Item);

            list[index] = item;

            ctx.Trigger(Exit);
            return Execution.Done;
        }
    }
}
