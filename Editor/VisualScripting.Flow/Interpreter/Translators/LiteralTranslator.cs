using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class LiteralTranslator : NodeTranslator<Literal>
    {
        protected override INode Translate(GraphBuilder builder, Literal unit, PortMapper mapping)
        {
            if (FlowGraphTranslator.TranslateConstant(builder, out var node, out mapping, unit.type, unit.value, unit.output, out _))
            {
                builder.AddNodeFromModel(unit, node, mapping);
                // TODO: find why this is here. v2/v3 maybe ?
                FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);
                return node;
            }

            return null;
        }
    }
}
