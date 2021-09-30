using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    class ToggleFlowNodeTranslator : NodeTranslator<ToggleFlow>
    {
        protected override INode Translate(GraphBuilder builder, ToggleFlow unit, PortMapper mapping)
        {
            var n = new ToggleFlowNode { StartOn = unit.startOn };
            mapping.AddSinglePort(builder, unit.enter, ref n.Enter);
            mapping.AddSinglePort(builder, unit.turnOn, ref n.TurnOn);
            mapping.AddSinglePort(builder, unit.turnOff, ref n.TurnOff);
            mapping.AddSinglePort(builder, unit.toggle, ref n.Toggle);
            mapping.AddSinglePort(builder, unit.exitOn, ref n.ExitOn);
            mapping.AddSinglePort(builder, unit.exitOff, ref n.ExitOff);
            mapping.AddSinglePort(builder, unit.turnedOn, ref n.TurnedOn);
            mapping.AddSinglePort(builder, unit.turnedOff, ref n.TurnedOff);
            mapping.AddSinglePort(builder, unit.isOn, ref n.IsOn);
            builder.AddNodeFromModel(unit, n, mapping);
            return n;
        }
    }
}
