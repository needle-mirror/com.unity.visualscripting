using System;
using System.Linq;
using Unity.VisualScripting.Interpreter;
using Unity.VisualScripting;

namespace Unity.VisualScripting
{
    internal class SelectOnEnumTranslator : NodeTranslator<SelectOnEnum>
    {
        protected override INode Translate(GraphBuilder builder, SelectOnEnum unit,
            PortMapper mapping)
        {
            var n = new SelectOnEnumNode();
            n.Values.SetCount(unit.branches.Count);
            n.EnumValues.SetCount(unit.branches.Count);

            mapping.AddSinglePort(builder, unit.selection, ref n.Selection);
            mapping.AddSinglePort(builder, unit.selector, ref n.Selector);
            mapping.AddMultiPort(builder, unit.branches.Values.Cast<IUnitPort>().ToList(), ref n.Values);
            mapping.AddMultiPort(builder, null, ref n.EnumValues);

            builder.AddNodeFromModel(unit, n, mapping);

            uint i = 0;
            foreach (var branch in unit.branches)
            {
                var branchKey = (Enum)branch.Key;
                FlowGraphTranslator.TranslateConstant(builder, out var enumBranchConstant, out var constantMapping, typeof(int), Convert.ToInt32(branchKey),
                    branch.Value, out var enumBranchConstantOutput);
                builder.AddNodeInternal(builder.GetNextNodeId(), enumBranchConstant, constantMapping);
                var inputPortIndex = n.EnumValues.SelectPort(i);
                builder.CreateEdge(enumBranchConstantOutput, inputPortIndex);
                i++;
            }
            return n;
        }
    }
}
