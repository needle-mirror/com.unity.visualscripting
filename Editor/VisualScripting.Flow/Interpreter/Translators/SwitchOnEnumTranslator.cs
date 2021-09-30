using System;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    internal class SwitchOnEnumTranslator : NodeTranslator<SwitchOnEnum>
    {
        protected override INode Translate(GraphBuilder builder, SwitchOnEnum unit,
            PortMapper mapping)
        {
            var n = new SwitchOnEnumNode();
            n.Branches.SetCount(unit.branches.Count);
            n.EnumValues.SetCount(unit.branches.Count);

            mapping.AddSinglePort(builder, unit.enter, ref n.Enter);
            mapping.AddSinglePort(builder, unit.@enum, ref n.Selector);

            mapping.AddMultiPort(builder, unit.branches.Values.Cast<IUnitPort>().ToList(), ref n.Branches);
            mapping.AddMultiPort(builder, null, ref n.EnumValues);

            builder.AddNodeFromModel(unit, n, mapping);

            uint i = 0;
            foreach (var branch in unit.branches)
            {
                Enum branchKey = branch.Key;
                FlowGraphTranslator.TranslateConstant(builder, out var enumBranchConstant, out var constantMapping, typeof(int),
                    Convert.ToInt32(branchKey),
                    null, out var enumBranchConstantOutput);

                NodeId enumBranchNodeId = builder.GetNextNodeId();
                builder.AddNodeInternal(enumBranchNodeId, enumBranchConstant, constantMapping);

                var inputPortIndex = n.EnumValues.SelectPort(i);
                builder.CreateEdge(enumBranchConstantOutput, inputPortIndex);
                i++;
            }

            return n;
        }
    }
    internal class SwitchOnIntegerTranslator : NodeTranslator<SwitchOnInteger>
    {
        protected override INode Translate(GraphBuilder builder, SwitchOnInteger unit,
            PortMapper mapping)
        {
            var n = new SwitchOnIntegerNode();
            n.Branches.SetCount(unit.branches.Count);
            n.ComparedValues.SetCount(unit.branches.Count);

            mapping.AddSinglePort(builder, unit.@default, ref n.Default);
            mapping.AddSinglePort(builder, unit.enter, ref n.Enter);
            mapping.AddSinglePort(builder, unit.selector, ref n.Selector);

            mapping.AddMultiPort(builder, unit.branches.Select(x => (IUnitPort)x.Value).ToList(), ref n.Branches);
            mapping.AddMultiPort(builder, null, ref n.ComparedValues);

            builder.AddNodeFromModel(unit, n, mapping);

            uint i = 0;
            foreach (var branch in unit.branches)
            {
                var branchKey = branch.Key;
                FlowGraphTranslator.TranslateConstant(builder, out var enumBranchConstant, out var constantMapping, typeof(int),
                    branchKey,
                    null, out var enumBranchConstantOutput);

                NodeId enumBranchNodeId = builder.GetNextNodeId();
                builder.AddNodeInternal(enumBranchNodeId, enumBranchConstant, constantMapping);

                var inputPortIndex = n.ComparedValues.SelectPort(i);
                builder.CreateEdge(enumBranchConstantOutput, inputPortIndex);
                i++;
            }

            return n;
        }
    }
    internal class SwitchOnStringTranslator : NodeTranslator<SwitchOnString>
    {
        protected override INode Translate(GraphBuilder builder, SwitchOnString unit,
            PortMapper mapping)
        {
            var n = new SwitchOnStringNode();
            n.Branches.SetCount(unit.branches.Count);
            n.ComparedValues.SetCount(unit.branches.Count);

            mapping.AddSinglePort(builder, unit.@default, ref n.Default);
            mapping.AddSinglePort(builder, unit.enter, ref n.Enter);
            mapping.AddSinglePort(builder, unit.selector, ref n.Selector);

            mapping.AddMultiPort(builder, unit.branches.Select(x => (IUnitPort)x.Value).ToList(), ref n.Branches);
            mapping.AddMultiPort(builder, null, ref n.ComparedValues);

            builder.AddNodeFromModel(unit, n, mapping);

            uint i = 0;
            foreach (var branch in unit.branches)
            {
                var branchKey = branch.Key;
                FlowGraphTranslator.TranslateConstant(builder, out var enumBranchConstant, out var constantMapping, typeof(string),
                    branchKey,
                    null, out var enumBranchConstantOutput);

                NodeId enumBranchNodeId = builder.GetNextNodeId();
                builder.AddNodeInternal(enumBranchNodeId, enumBranchConstant, constantMapping);

                var inputPortIndex = n.ComparedValues.SelectPort(i);
                builder.CreateEdge(enumBranchConstantOutput, inputPortIndex);
                i++;
            }

            return n;
        }
    }
}
