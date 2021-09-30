using System;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class SubGraphTranslator : NodeTranslator<SubgraphUnit>
    {
        protected override INode Translate(GraphBuilder builder, SubgraphUnit unit, PortMapper mapping)
        {
            var nestedGraphAssetIndex = builder.GetTranslatedGraphAsset(unit);
            var n = new SubGraphNode
            {
                InputTriggers = { DataCount = unit.controlInputs.Count },
                OutputTriggers = { DataCount = unit.controlOutputs.Count },
                InputDatas = { DataCount = unit.valueInputs.Count },
                OutputDatas = { DataCount = unit.valueOutputs.Count },
                NestedGraphAssetIndex = nestedGraphAssetIndex,
            };
            mapping.AddMultiPortIndexed(builder, i => unit.controlInputs[i], ref n.InputTriggers);
            mapping.AddMultiPortIndexed(builder, i => unit.controlOutputs[i], ref n.OutputTriggers);
            mapping.AddMultiPortIndexed(builder, i => unit.valueInputs[i], ref n.InputDatas);
            mapping.AddMultiPortIndexed(builder, i => unit.valueOutputs[i], ref n.OutputDatas);
            builder.AddNodeFromModel(unit, n, mapping);

            return n;
        }
    }
    [UsedImplicitly]
    internal class GraphInputTranslator : NodeTranslator<GraphInput>
    {
        protected override INode Translate(GraphBuilder builder, GraphInput unit, PortMapper mapping)
        {
            var n = new GraphInputNode
            {
                Triggers = { DataCount = unit.controlOutputs.Count },
                Datas = { DataCount = unit.valueOutputs.Count },
            };
            mapping.AddMultiPortIndexed(builder, i => unit.controlOutputs[i], ref n.Triggers);
            mapping.AddMultiPortIndexed(builder, i => unit.valueOutputs[i], ref n.Datas, i =>
            {
                if (unit.graph.valueInputDefinitions[(int)i].hasDefaultValue)
                    return Value.FromObject(unit.graph.valueInputDefinitions[(int)i].defaultValue);
                return null;
            });

            builder.AddNodeFromModel(unit, n, mapping);
            return n;
        }
    }
    [UsedImplicitly]
    internal class GraphOutputTranslator : NodeTranslator<GraphOutput>
    {
        protected override INode Translate(GraphBuilder builder, GraphOutput unit, PortMapper mapping)
        {
            var n = new GraphOutputNode
            {
                Triggers = { DataCount = unit.controlInputs.Count },
                Datas = { DataCount = unit.valueInputs.Count },
            };
            mapping.AddMultiPortIndexed(builder, i => unit.controlInputs[i], ref n.Triggers);
            mapping.AddMultiPortIndexed(builder, i => unit.valueInputs[i], ref n.Datas);
            builder.AddNodeFromModel(unit, n, mapping);
            return n;
        }
    }
}
