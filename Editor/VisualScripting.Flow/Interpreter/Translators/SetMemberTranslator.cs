using System;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class SetMemberTranslator : NodeTranslator<SetMember>
    {
        protected override INode Translate(GraphBuilder builder, SetMember unit, PortMapper mapping)
        {
            INode node;
            if (!GraphTranslationCallbackReceiver.SetMemberModelToRuntimeMapping.TryGetValue(unit.member.ToUniqueString(),
                out var runtimeType) || (builder.Options & TranslationOptions.ForceApiReflectionNodes) != 0)
            {
                uint memberIdx = builder.AddReflectedMember(unit.member);
                var setMemberReflectionNode = new SetMemberReflectionNode { ReflectedMemberIndex = memberIdx };

                mapping = new PortMapper();
                mapping.AddSinglePort(builder, unit.assign, ref setMemberReflectionNode.Assign);
                mapping.AddSinglePort(builder, unit.assigned, ref setMemberReflectionNode.Assigned);
                mapping.AddSinglePort(builder, unit.input, ref setMemberReflectionNode.Input);
                mapping.AddSinglePort(builder, unit.output, ref setMemberReflectionNode.Output);
                if (unit.member.requiresTarget)
                {
                    mapping.AddSinglePort(builder, unit.target, ref setMemberReflectionNode.Target);
                    mapping.AddSinglePort(builder, unit.targetOutput, ref setMemberReflectionNode.TargetOutput);
                }

                node = setMemberReflectionNode;
                builder.AddNodeFromModel(unit, node, mapping);
                FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);

                return node;
            }


            node = (INode)Activator.CreateInstance(runtimeType);
            mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);

            builder.AddNodeFromModel(unit, node, mapping);
            FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);
            return node;
        }
    }
}
