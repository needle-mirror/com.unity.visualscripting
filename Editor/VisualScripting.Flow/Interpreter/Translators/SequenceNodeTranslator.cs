using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    class SequenceNodeTranslator : NodeTranslator<Sequence>
    {
        protected override INode Translate(GraphBuilder builder, Sequence unit, PortMapper mapping)
        {
            var n = new SequenceNode { Outputs = { DataCount = unit.outputCount } };
            mapping.AddSinglePort(builder, unit.enter, ref n.Enter);
            mapping.AddMultiPortIndexed(builder, i => unit.multiOutputs[i], ref n.Outputs);
            builder.AddNodeFromModel(unit, n, mapping);
            return n;
        }
    }
}
