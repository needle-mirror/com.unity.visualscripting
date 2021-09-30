using System;

namespace Unity.VisualScripting.Interpreter
{
    class GetMemberCodeGenerator : NodeCodeGenerator<GetMember>
    {
        protected override bool GenerateCode(GetMember unit, out string fileName, out string code)
        {
            var member = unit.member;
            var ports = string.Empty;

            fileName = FormatFileName($"{member.targetTypeName}_{member.name}") + "_get";

            // TODO figure out a way to hint if the member is a constant (Quaternion.identity is, Time.deltaTime isn't)
            // if (member.isGettable && !member.isSettable) nodeType = typeof(IConstantNode);
            CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Data, "Value");

            var call = $"{CodeGeneratorUtils.CallTarget(ref ports, member)}.{member.name}";
            call = CodeGeneratorUtils.WrapValue(member.type, call);
            var body = $"        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance => ctx.Write(Value, {call});";
            code = CodeGeneratorUtils.TemplateNode(
                fileName,
                ConstantFoldingTransform.FoldableData.IsFoldable(member) ? typeof(IConstantNode) : typeof(IDataNode),
                ports,
                body,
                MemberNodeAttribute.ToString(unit.GetType(), member));
            return true;
        }

        protected override bool ShouldGenerateCode(GetMember unit, TranslationOptions options)
        {
            if (!GraphTranslationCallbackReceiver.InvokeMemberModelToRuntimeMapping.TryGetValue(unit.member.ToUniqueString(), out var runtimeType)
                || (options & TranslationOptions.ForceApiReflectionNodes) != 0)
            {
                if (runtimeType == null)
                    return true;
            }

            return false;
        }
    }
}
