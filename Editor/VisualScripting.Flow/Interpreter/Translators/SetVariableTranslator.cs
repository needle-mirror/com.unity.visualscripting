using System;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class SetVariableTranslator : NodeTranslator<SetVariable>
    {
        protected override INode Translate(GraphBuilder builder, SetVariable unit, PortMapper mapping)
        {
            INode node;
            switch (unit.kind)
            {
                case VariableKind.Graph:
                case VariableKind.Flow:
                    var variableDataIndex = builder.GetVariableDataIndex(unit);
                    node = new Interpreter.SetVariableNode { VariableHandle = variableDataIndex.DataIndex };
                    mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);
                    break;
                case VariableKind.Object:
                    node = new Interpreter.SetObjectVariableNode { VariableName = (string)unit.defaultValues[unit.name.key] };
                    mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);
                    break;
                case VariableKind.Scene:
                    node = new Interpreter.SetSceneVariableNode { VariableName = (string)unit.defaultValues[unit.name.key] };
                    mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);
                    break;
                case VariableKind.Application:
                    node = new Interpreter.SetApplicationVariableNode { VariableName = (string)unit.defaultValues[unit.name.key] };
                    mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);
                    break;
                case VariableKind.Saved:
                    node = new Interpreter.SetSavedVariableNode { VariableName = (string)unit.defaultValues[unit.name.key] };
                    mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit.kind), "Unsupported variable type: " + unit.kind);
            }

            builder.AddNodeFromModel(unit, node, mapping);
            FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);
            return node;
        }
    }
}
