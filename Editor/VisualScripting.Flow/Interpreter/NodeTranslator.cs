using System;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;
using UnityEngine.Assertions;

namespace Unity.VisualScripting
{
    interface INodeTranslator
    {
        void TranslateUnit(GraphBuilder builder, IUnit unit, out INode node, out PortMapper mapping);
        Type TranslatedUnitType { get; }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    abstract class NodeTranslator<T> : INodeTranslator
    {
        public void TranslateUnit(GraphBuilder builder, IUnit unit, out INode node, out PortMapper mapping)
        {
            mapping = new PortMapper();

            node = Translate(builder, (T)unit, mapping);

            if (node != null)
                Assert.IsTrue(builder.NodeHasBeenAdded(node), $"{GetType().Name} must add the node {node} to the graph builder using {nameof(builder.AddNodeInternal)} or {nameof(builder.AddNodeFromModel)}");
        }

        public Type TranslatedUnitType => typeof(T);

        protected abstract INode Translate(GraphBuilder builder, T unit, PortMapper mapping);
    }
}
