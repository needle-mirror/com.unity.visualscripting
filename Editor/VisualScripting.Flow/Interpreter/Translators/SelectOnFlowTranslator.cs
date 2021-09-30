using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    internal class SelectOnFlowTranslator : NodeTranslator<SelectOnFlow>
    {
        protected override INode Translate(GraphBuilder builder, SelectOnFlow unit,
            PortMapper mapping)
        {
            var n = new SelectOnFlowNode();

            n.Inputs.SetCount(unit.branchCount);
            n.Values.SetCount(unit.branchCount);

            mapping.AddSinglePort(builder, unit.exit, ref n.Exit);
            mapping.AddSinglePort(builder, unit.selection, ref n.Selection);
            mapping.AddMultiPortIndexed(builder, unit.GetBranchControlInput, ref n.Inputs);
            mapping.AddMultiPortIndexed(builder, unit.GetBranchValueInput, ref n.Values);

            builder.AddNodeFromModel(unit, n, mapping);

            return n;
        }
    }
}
