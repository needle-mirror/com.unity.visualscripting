using System;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class GetMemberTranslator : NodeTranslator<GetMember>
    {
        protected override INode Translate(GraphBuilder builder, GetMember unit, PortMapper mapping)
        {
            INode node;

            if (!GetSpecialzedTypeOrFallbackReflectionNode(builder, unit.member, out Type runtimeType, out GetMemberReflectionNode getMemberReflectionNode))
            {
                mapping = new PortMapper();
                mapping.AddSinglePort(builder, unit.value, ref getMemberReflectionNode.Value);
                if (unit.member.requiresTarget)
                    mapping.AddSinglePort(builder, unit.target, ref getMemberReflectionNode.Target);

                node = getMemberReflectionNode;
            }
            else
            {
                node = (INode)Activator.CreateInstance(runtimeType);
                mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);
            }

            builder.AddNodeFromModel(unit, node, mapping);
            FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);
            return node;
        }

        internal static bool GetSpecialzedTypeOrFallbackReflectionNode(GraphBuilder builder, Member unitMember, out Type specializedNodeType, out GetMemberReflectionNode getMemberReflectionNode)
        {
            specializedNodeType = null;
            if ((builder.Options & TranslationOptions.ForceApiReflectionNodes) != 0 || !GraphTranslationCallbackReceiver.InvokeMemberModelToRuntimeMapping.TryGetValue(unitMember.ToUniqueString(),
                out specializedNodeType))
            {
                uint memberIdx = builder.AddReflectedMember(unitMember);
                getMemberReflectionNode = new GetMemberReflectionNode { ReflectedMemberIndex = memberIdx };
                return false;
            }

            getMemberReflectionNode = default;
            return true;
        }
    }
}
