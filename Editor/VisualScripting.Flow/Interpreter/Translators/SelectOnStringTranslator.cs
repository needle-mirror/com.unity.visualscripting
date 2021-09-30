using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    internal class SelectOnStringTranslator : NodeTranslator<SelectOnString>
    {
        protected override INode Translate(GraphBuilder builder, SelectOnString unit, PortMapper mapping)
        {
            var n = new SelectOnStringNode { IgnoreCase = unit.ignoreCase };
            n.OptionPorts.SetCount(unit.branches.Count);
            n.OptionValues.SetCount(unit.branches.Count);

            mapping.AddSinglePort(builder, unit.selector, ref n.Selector);
            mapping.AddSinglePort(builder, unit.@default, ref n.Default);
            mapping.AddSinglePort(builder, unit.selection, ref n.Selection);
            mapping.AddMultiPortIndexed(builder, i => unit.branches[i].Value, ref n.OptionPorts);
            mapping.AddMultiPort(builder, null, ref n.OptionValues);

            builder.AddNodeFromModel(unit, n, mapping);

            for (uint i = 0; i < unit.branches.Count; i++)
            {
                FlowGraphTranslator.TranslateConstant(builder, out var enumBranchConstant, out var constantMapping, typeof(string), unit.branches[(int)i].Key,
                    null, out var enumBranchConstantOutput);
                builder.AddNodeInternal(builder.GetNextNodeId(), enumBranchConstant, constantMapping);
                var inputPortIndex = n.OptionValues.SelectPort(i);
                builder.CreateEdge(enumBranchConstantOutput, inputPortIndex);
            }

            return n;
        }
    }
}
