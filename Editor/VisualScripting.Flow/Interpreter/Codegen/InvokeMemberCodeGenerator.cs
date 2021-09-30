using System;
using System.Linq;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class InvokeMemberCodeGenerator : NodeCodeGenerator<InvokeMember>
    {
        protected override bool GenerateCode(InvokeMember unit, out string fileName, out string code)
        {
            code = null;
            var member = unit.member;
            Type[] nodeType;
            var ports = string.Empty;

            fileName = FormatFileName($"{member.targetTypeName}_{member.name}");
            fileName += "_" + GetStableHashCode(member);

            if (member.isGettable) // has return type
            {
                nodeType = ConstantFoldingTransform.FoldableData.IsFoldable(member) ? new[] { typeof(IDataNode), typeof(IFoldableNode) } : new[] { typeof(IDataNode) };
                CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Data, "Result", portDescriptionName: nameof(InvokeMember.result));
            }
            else
            {
                nodeType = new[] { typeof(IFlowNode) };
                CodeGeneratorUtils.AddPort(ref ports, PortDirection.Input, PortType.Trigger, "Enter");
                CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Trigger, "Exit");
            }

            var call = CodeGeneratorUtils.CallTarget(ref ports, member);
            var parameterInfos = member.GetParameterInfos().ToArray();
            var paramsStr = string.Empty;
            var preCall = string.Empty;
            var postCall = string.Empty;

            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var p = parameterInfos[i];
                CodeGeneratorUtils.AddPort(ref ports, p.IsOut ? PortDirection.Output : PortDirection.Input, PortType.Data, p.Name, portDescriptionName: (p.IsOut ? '&' : '%') + p.Name);
                if (i > 0)
                    paramsStr += ", ";
                if (!p.IsOut && p.ParameterType.IsByRef)
                {
                    CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Data, "out" + p.Name, portDescriptionName: '&' + p.Name);

                    preCall += $"var ref{p.Name} = {CodeGeneratorUtils.GetReadCall(p.ParameterType.GetElementType(), p.Name, false)}";
                    paramsStr += $"ref ref{p.Name}";
                    postCall += $"            ctx.Write(out{p.Name}, {CodeGeneratorUtils.WrapValue(p.ParameterType.GetElementType(), "ref" + p.Name)});";
                }
                else if (p.IsOut)
                {
                    paramsStr += "out var out" + p.Name;
                    postCall += $"            ctx.Write({p.Name}, {CodeGeneratorUtils.WrapValue(p.ParameterType, "out" + p.Name)});";
                }
                else
                    paramsStr += CodeGeneratorUtils.GetReadCall(p.ParameterType, p.Name, false);
            }

            if (member.isConstructor)
                call = $"new {call}({paramsStr})";
            else
                call += $".{member.name}({paramsStr})";
            if (member.isGettable)
                call = $"ctx.Write(Result, {CodeGeneratorUtils.WrapValue(member.type, call)})";

            if (postCall != String.Empty)
                call += $";{Environment.NewLine}{postCall}";

            if (preCall != String.Empty)
                call = $"{preCall};{Environment.NewLine}            {call}";

            var body = nodeType[0] == typeof(IDataNode)
                ? $@"        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {{
            {call};
        }}"
                : $@"        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {{
            {call};
            ctx.Trigger(Exit);
            return Execution.Done;
        }}";
            code = CodeGeneratorUtils.TemplateNode(
                fileName,
                nodeType,
                ports,
                body,
                MemberNodeAttribute.ToString(unit.GetType(), member));
            return true;
        }

        protected override bool ShouldGenerateCode(InvokeMember unit, TranslationOptions options)
        {
            if (!GraphTranslationCallbackReceiver.InvokeMemberModelToRuntimeMapping.TryGetValue(unit.member.ToUniqueString(), out var runtimeType)
                || (options & TranslationOptions.ForceApiReflectionNodes) != 0)
            {
                if (runtimeType == null)
                    return true;
            }

            return false;
        }

        static ulong GetStableHashCode(Member member)
        {
            unchecked
            {
                ulong hash = 17;


                hash = hash * 23 + (member.targetType == null ? 0 : TypeHash.CalculateStableTypeHash(member.targetType));
                hash = hash * 23 + (member.name == null ? 0 : TypeHash.FNV1A64(member.name));

                if (member.parameterTypes != null)
                {
                    foreach (var parameterType in member.parameterTypes)
                    {
                        hash = hash * 23 + TypeHash.CalculateStableTypeHash(parameterType);
                    }
                }
                else
                {
                    hash = hash * 23 + 0;
                }

                return hash;
            }
        }
    }
}
