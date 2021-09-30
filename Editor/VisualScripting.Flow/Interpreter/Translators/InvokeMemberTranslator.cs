using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class InvokeMemberTranslator : NodeTranslator<InvokeMember>
    {
        protected override INode Translate(GraphBuilder builder, InvokeMember unit, PortMapper mapping)
        {
            var apiNodeIsUsedAsFlowNode = unit.enter.hasValidConnection;
            INode node;

            if (!GraphTranslationCallbackReceiver.InvokeMemberModelToRuntimeMapping.TryGetValue(unit.member.ToUniqueString(),
                out var runtimeType) || (builder.Options & TranslationOptions.ForceApiReflectionNodes) != 0)
            {
                uint memberIdx = builder.AddReflectedMember(unit.member);

                if (unit.member.type == typeof(void))
                {
                    var invokeMemberReflectionNode = new InvokeMemberVoidReflectionNode { ReflectedMemberIndex = memberIdx };
                    mapping.AddSinglePort(builder, unit.enter, ref invokeMemberReflectionNode.Enter);
                    mapping.AddSinglePort(builder, unit.exit, ref invokeMemberReflectionNode.Exit);
                    if (unit.member.requiresTarget)
                    {
                        mapping.AddSinglePort(builder, unit.target, ref invokeMemberReflectionNode.Target);
                        mapping.AddSinglePort(builder, unit.targetOutput, ref invokeMemberReflectionNode.TargetOutput);
                    }

                    invokeMemberReflectionNode.Input.DataCount = unit.inputParameters.Count;
                    invokeMemberReflectionNode.Output.DataCount = unit.outputParameters.Count;
                    mapping.AddMultiPort(builder, unit.inputParameters.Values.Cast<IUnitPort>().ToList(), ref invokeMemberReflectionNode.Input);

                    mapping.AddMultiPort(builder, unit.outputParameters.Values.Cast<IUnitPort>().ToList(), ref invokeMemberReflectionNode.Output);

                    node = invokeMemberReflectionNode;
                    var nextNodeId = builder.GetNextNodeId();
                    builder.AddNodeInternal(nextNodeId, node, mapping, unit);
                }
                else
                {
                    var invokeMemberReflectionNode = new InvokeMemberReflectionNode { ReflectedMemberIndex = memberIdx };

                    var cacheMapping = new PortMapper();

                    CacheNode cacheNode = default;
                    NodeId cacheNodeId = default;
                    if (apiNodeIsUsedAsFlowNode)
                    {
                        cacheNode = new CacheNode();
                        cacheNode.CopiedOutputs.DataCount = unit.outputParameters.Count + (unit.member.isGettable ? 1 : 0); // return value
                        cacheNode.CopiedInputs.DataCount = cacheNode.CopiedOutputs.DataCount;

                        cacheMapping.AddSinglePort(builder, unit.enter, ref cacheNode.Enter);
                        cacheMapping.AddSinglePort(builder, unit.exit, ref cacheNode.Exit);
                        // map cache input, no unit port
                        cacheMapping.AddMultiPort(builder, null, ref cacheNode.CopiedInputs);
                        // map cache output to unit port
                        List<IUnitPort> cacheOutputs = new List<IUnitPort>();
                        if (unit.member.isGettable)
                            cacheOutputs.Add(unit.result);

                        cacheOutputs.AddRange(unit.outputParameters.Values);
                        cacheMapping.AddMultiPort(builder, cacheOutputs, ref cacheNode.CopiedOutputs);

                        builder.AddNodeInternal(cacheNodeId = builder.GetNextNodeId(), cacheNode, cacheMapping, unit);
                    }

                    mapping = new PortMapper();
                    if (unit.member.isGettable)
                        mapping.AddSinglePort(builder, apiNodeIsUsedAsFlowNode ? null : unit.result, ref invokeMemberReflectionNode.Result);
                    if (unit.member.requiresTarget)
                    {
                        mapping.AddSinglePort(builder, unit.target, ref invokeMemberReflectionNode.Target);
                        mapping.AddSinglePort(builder, unit.targetOutput, ref invokeMemberReflectionNode.TargetOutput);
                    }

                    invokeMemberReflectionNode.Input.DataCount = unit.inputParameters.Count;
                    invokeMemberReflectionNode.Output.DataCount = unit.outputParameters.Count;
                    mapping.AddMultiPort(builder, unit.inputParameters.Values.Cast<IUnitPort>().ToList(), ref invokeMemberReflectionNode.Input);

                    mapping.AddMultiPort(builder, apiNodeIsUsedAsFlowNode ? null : unit.outputParameters.Values.Cast<IUnitPort>().ToList(), ref invokeMemberReflectionNode.Output);

                    node = invokeMemberReflectionNode;
                    var nextNodeId = builder.GetNextNodeId();
                    builder.AddNodeInternal(nextNodeId, node, mapping, unit);
                    if (apiNodeIsUsedAsFlowNode)
                    {
                        for (uint i = 0; i < cacheNode.CopiedInputs.DataCount; i++)
                            builder.CreateEdge(unit.member.isGettable && i == 0
                                ? invokeMemberReflectionNode.Result
                                : invokeMemberReflectionNode.Output.SelectPort((uint)(i - (unit.member.isGettable ? 1 : 0))),
                                cacheNode.CopiedInputs.SelectPort(i));
                    }
                }
            }
            else
            {
                node = (INode)Activator.CreateInstance(runtimeType);

                if (node is IDataNode && apiNodeIsUsedAsFlowNode)
                {
                    FlowGraphTranslator.TranslateInvokeMemberAsDataNodeAndCacheNode(unit, builder, node, unit, out mapping);
                }
                else
                {
                    mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);
                    builder.AddNodeFromModel(unit, node, mapping);
                }
            }

            FlowGraphTranslator.TranslateEmbeddedConstants(unit, builder, mapping);

            return node;
        }
    }
}
