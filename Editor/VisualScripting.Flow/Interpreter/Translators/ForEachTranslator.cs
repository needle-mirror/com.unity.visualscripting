using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    class ForEachTranslator : NodeTranslator<ForEach>
    {
        protected override INode Translate(GraphBuilder builder, ForEach unit, PortMapper mapping)
        {
            var node = new ForEachNode { Dictionary = unit.dictionary };
            mapping.AddSinglePort(builder, unit.enter, ref node.Enter);
            mapping.AddSinglePort(builder, unit.exit, ref node.Exit);
            mapping.AddSinglePort(builder, unit.body, ref node.Body);
            mapping.AddSinglePort(builder, unit.collection, ref node.Collection);
            mapping.AddSinglePort(builder, unit.currentIndex, ref node.CurrentIndex);
            if (unit.dictionary)
                mapping.AddSinglePort(builder, unit.currentKey, ref node.CurrentKey);
            mapping.AddSinglePort(builder, unit.currentItem, ref node.CurrentItem);
            builder.AddNodeFromModel(unit, node, mapping);
            return node;
        }
    }
}
