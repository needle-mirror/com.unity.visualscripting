using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class ExposeTranslator : NodeTranslator<Expose>
    {
        protected override INode Translate(GraphBuilder builder, Expose unit, PortMapper mapping)
        {
            var needTarget = unit.members.Any(m => m.Value.requiresTarget && m.Key.hasValidConnection);
            PassthroughNode passthrough = default;
            if (needTarget)
            {
                passthrough = PassthroughNode.Create(1);
                mapping.AddMultiPortIndexed(builder, i => unit.target, ref passthrough.Input);
                mapping.AddMultiPort(builder, null, ref passthrough.Output);
                builder.AddNodeFromModel(unit, passthrough, mapping);
            }

            foreach (var member in unit.members)
            {
                if (!member.Key.hasValidConnection)
                    continue;
                var memberMapping = new PortMapper();
                INode node;
                if (!GetMemberTranslator.GetSpecialzedTypeOrFallbackReflectionNode(builder, member.Value,
                    out var specializedNodeType, out var reflectionNode))
                {
                    builder.AddUnitToCodegen(new GetMember(member.Value));

                    if (member.Value.requiresTarget)
                    {
                        memberMapping.AddSinglePort(builder, unit.target, ref reflectionNode.Target);
                        builder.CreateEdge(passthrough.Output.SelectPort(0), reflectionNode.Target);
                    }
                    memberMapping.AddSinglePort(builder, member.Key, ref reflectionNode.Value);
                    node = reflectionNode;
                }
                else
                {
                    node = (INode)Activator.CreateInstance(specializedNodeType);
                    var output = FlowGraphTranslator.GetNodeOutputPorts(node).Single();
                    node = builder.AutoAssignPortIndicesAndMapPorts(node, memberMapping, new Dictionary<IUnitPort, FieldInfo> { [member.Key] = output.Item1 });
                    if (member.Value.requiresTarget)
                    {
                        builder.CreateEdge(passthrough.Output.SelectPort(0), FlowGraphTranslator.GetNodeInputPorts(node).Single().Item2);
                    }
                }

                builder.AddNodeFromModel(unit, node, memberMapping);
            }

            return null;
        }
    }
}
