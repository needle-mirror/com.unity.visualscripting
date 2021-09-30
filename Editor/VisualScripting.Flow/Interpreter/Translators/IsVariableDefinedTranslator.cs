using System;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class IsVariableDefinedTranslator : NodeTranslator<IsVariableDefined>
    {
        protected override INode Translate(GraphBuilder builder, IsVariableDefined unit, PortMapper mapping)
        {
            switch (unit.kind)
            {
                case VariableKind.Graph:
                case VariableKind.Object:
                case VariableKind.Scene:
                case VariableKind.Application:
                case VariableKind.Saved:
                case VariableKind.Flow:
                    var node = new IsVariableDefinedNode { Kind = unit.kind };
                    mapping.AddSinglePort(builder, unit.isVariableDefined, ref node.Value);
                    return node;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit.kind), "Unsupported variable type: " + unit.kind);
            }
        }
    }
}
