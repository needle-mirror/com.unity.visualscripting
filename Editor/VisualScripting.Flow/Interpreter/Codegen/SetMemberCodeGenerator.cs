namespace Unity.VisualScripting.Interpreter
{
    class SetMemberCodeGenerator : NodeCodeGenerator<SetMember>
    {
        protected override bool GenerateCode(SetMember unit, out string fileName, out string code)
        {
            var member = unit.member;
            var ports = string.Empty;

            fileName = FormatFileName($"{member.targetTypeName}_{member.name}") + "_set";

            CodeGeneratorUtils.AddPort(ref ports, PortDirection.Input, PortType.Trigger, "Assign");
            CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Trigger, "Assigned");
            CodeGeneratorUtils.AddPort(ref ports, PortDirection.Input, PortType.Data, "Input");
            CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Data, "Output");
            var call = $"{CodeGeneratorUtils.CallTarget(ref ports, member)}.{member.name}";
            var body =
                $@"        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {{
            var val = {CodeGeneratorUtils.GetReadCall(unit.member.type, "Input", false)};
            {call} = val;
            ctx.Write(Output, {(unit.member.type.IsClass ? "Interpreter.Value.FromObject(val)" : "val")});
            ctx.Trigger(Assigned);
            return Execution.Done;
        }}";

            code = CodeGeneratorUtils.TemplateNode(
                fileName,
                typeof(IFlowNode),
                ports,
                body,
                MemberNodeAttribute.ToString(unit.GetType(), member));
            return true;
        }

        protected override bool ShouldGenerateCode(SetMember unit, TranslationOptions options)
        {
            if (!GraphTranslationCallbackReceiver.SetMemberModelToRuntimeMapping.TryGetValue(unit.member.ToUniqueString(), out var runtimeType)
                || (options & TranslationOptions.ForceApiReflectionNodes) != 0)
            {
                if (runtimeType == null)
                    return true;
            }

            return false;
        }
    }
}
