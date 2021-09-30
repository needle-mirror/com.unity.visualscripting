using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    class ToggleValueNodeTranslator : NodeTranslator<ToggleValue>
    {
        protected override INode Translate(GraphBuilder builder, ToggleValue unit, PortMapper mapping)
        {
            // the unit data ports work like a data node: when pulling on Value, it pulls on OnValue or OffValue according to IsOn
            // so create two nodes:
            // - ToggleValueNode is a flow node with one output data port: Value
            // - one Select node whose Condition is the toggle's Value port, and IfTrue/IfFalse are connected to the unit's
            // OnValue/OffValue
            var n = new ToggleValueNode { StartOn = unit.startOn };
            mapping.AddSinglePort(builder, unit.turnOn, ref n.TurnOn);
            mapping.AddSinglePort(builder, unit.turnOff, ref n.TurnOff);
            mapping.AddSinglePort(builder, unit.toggle, ref n.Toggle);
            mapping.AddSinglePort(builder, unit.turnedOn, ref n.TurnedOn);
            mapping.AddSinglePort(builder, unit.turnedOff, ref n.TurnedOff);
            mapping.AddSinglePort(builder, unit.isOn, ref n.IsOn);
            builder.AddNodeFromModel(unit, n, mapping);

            var select = new SelectNode();
            var selectMapping = new PortMapper();
            selectMapping.AddSinglePort(builder, null, ref select.Condition);
            selectMapping.AddSinglePort(builder, unit.onValue, ref select.IfTrue);
            selectMapping.AddSinglePort(builder, unit.offValue, ref select.IfFalse);
            selectMapping.AddSinglePort(builder, unit.value, ref select.Selection);
            builder.AddNodeFromModel(unit, select, selectMapping);

            builder.CreateEdge(n.IsOn, select.Condition);
            return n;
        }
    }
}
