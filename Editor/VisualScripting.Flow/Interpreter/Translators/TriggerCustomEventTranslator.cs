using System;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    class CustomEventTranslator : NodeTranslator<CustomEvent>
    {
        protected override INode Translate(GraphBuilder builder, CustomEvent unit, PortMapper mapping)
        {
            var n = new CustomEventNode { Arguments = { DataCount = unit.argumentCount } };
            mapping.AddSinglePort(builder, unit.trigger, ref n.Trigger);
            mapping.AddSinglePort(builder, unit.name, ref n.Name);
            mapping.AddSinglePort(builder, unit.target, ref n.Target);
            mapping.AddMultiPortIndexed(builder, i => unit.argumentPorts[i], ref n.Arguments);
            builder.AddNodeFromModel(unit, n, mapping);

            FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);
            return n;
        }
    }
    class TriggerCustomEventTranslator : NodeTranslator<TriggerCustomEvent>
    {
        protected override INode Translate(GraphBuilder builder, TriggerCustomEvent unit, PortMapper mapping)
        {
            var n = new TriggerCustomEventNode { Arguments = { DataCount = unit.argumentCount } };
            mapping.AddSinglePort(builder, unit.enter, ref n.Enter);
            mapping.AddSinglePort(builder, unit.exit, ref n.Exit);
            mapping.AddSinglePort(builder, unit.name, ref n.Name);
            mapping.AddSinglePort(builder, unit.target, ref n.Target);
            mapping.AddMultiPortIndexed(builder, i => unit.arguments[i], ref n.Arguments);
            builder.AddNodeFromModel(unit, n, mapping);

            FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);
            return n;
        }
    }
}
