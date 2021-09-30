using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;
using ValueType = Unity.VisualScripting.Interpreter.ValueType;

namespace Unity.VisualScripting
{
    public class FlowGraphTranslator
    {
        private static Dictionary<Type, (MemberInfo enumMember, Dictionary<object, Type>)> s_ModelVariantToRuntimeMapping;
        private static Dictionary<Type, Type> s_ModelToRuntimeMapping;
        private static Dictionary<Type, INodeTranslator> s_UnitToTranslatorMapping;

        static ValueType FromType(Type t)
        {
            switch (t)
            {
                case Type _ when t == typeof(bool):
                    return ValueType.Bool;
                case Type _ when t == typeof(int):
                    return ValueType.Int;
                case Type _ when t == typeof(float):
                    return ValueType.Float;
                case Type _ when t == typeof(Vector2):
                    return ValueType.Float2;
                case Type _ when t == typeof(Vector3):
                    return ValueType.Float3;
                case Type _ when t == typeof(Vector4):
                    return ValueType.Float4;
                case Type _ when t == typeof(Quaternion):
                    return ValueType.Quaternion;
                case Type _ when t == typeof(Color):
                    return ValueType.Color;
                case Type _ when t.IsEnum:
                    return ValueType.Enum;
                default:
                    return ValueType.ManagedObject;
            }
        }

        public static BindingId BindingIdFromVariable(string name, VariableKind kind) => BindingId.From((ulong)name.GetHashCode(), (ulong)kind);

        internal List<GraphTransform> Transforms;

        public void Translate(FlowGraphContext flowGraphContext, FlowGraph flowGraph,
            RuntimeGraphAsset runtimeGraphAsset, List<Warning> messages, TranslationOptions options = TranslationOptions.Default)
        {
            InitCache();

            GraphBuilder builder = new GraphBuilder(flowGraphContext);

            foreach (var flowGraphVariable in flowGraph.variables)
            {
                var type = string.IsNullOrEmpty(flowGraphVariable.typeHandle.Identification)
                    ? flowGraphVariable.value?.GetType()
                    : flowGraphVariable.typeHandle.Resolve();

                var _ = builder.DeclareVariable(VariableType.Variable, FromType(type), BindingIdFromVariable(flowGraphVariable.name, VariableKind.Graph), flowGraphVariable.name, Value.FromObject(flowGraphVariable.value));
            }

            foreach (var unit in flowGraph.elements.OfType<IUnit>())
            {
                // skip disconnected units
                var unitAnalysis = unit.Analysis<UnitAnalysis>(flowGraphContext);
                if ((options & TranslationOptions.TranslateUnusedNodes) == 0 && !unitAnalysis.isEntered)
                    continue;

                void SelectAction() => flowGraphContext.selection.Select(unit);

                if (unitAnalysis.warnings.Any())
                {
                    messages.AddRange(unitAnalysis.warnings.Select(w => w.exception != null
                        ? new Warning(w.exception, "Select Unit", SelectAction)
                        : new Warning(w.level,
                            $"Unit error: {w.message}", "Select unit", SelectAction)));
                }

                try
                {
                    if (unit is UnifiedVariableUnit unifiedVariableUnit)
                    {
                        var name = Flow.FetchValue<string>(unifiedVariableUnit.name, flowGraphContext.reference);
                        var bindingId = BindingIdFromVariable(name, unifiedVariableUnit.kind);
                        builder.AddVariableUnit(unifiedVariableUnit, bindingId);


                    }

                    TranslateNode(unit, builder, out _, out _, options,
                        (level, s) => LogMessage(unit, level, s));
                }
                catch (Exception e)
                {
                    string unitMsg = unit is MemberUnit memberUnit
                        ? memberUnit.member.ToUniqueString()
                        : unit.ToString();
                    LogMessage(unit, WarningLevel.Error, $"Error during the translation of the unit '{unitMsg}':\n{e}");
                }
            }


            // Edges
            foreach (var connection in flowGraph.elements.OfType<IUnitConnection>())
            {
                if ((options & TranslationOptions.TranslateUnusedNodes) == 0 &&
                    (!connection.source.unit.Analysis<UnitAnalysis>(flowGraphContext).isEntered ||
                     !connection.destination.unit.Analysis<UnitAnalysis>(flowGraphContext).isEntered))
                    continue;
                try
                {
                    switch (connection)
                    {
                        case ControlConnection controlConnection:
                            builder.CreateEdge(controlConnection.source, controlConnection.destination);
                            break;
                        case ValueConnection valueConnection:
                            builder.CreateEdge(valueConnection.source, valueConnection.destination);
                            break;
                        default:
                            throw new NotImplementedException(connection.GetType().ToString());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.ToString());
                }
            }

            if (Transforms != null)
            {
                foreach (var graphTransform in Transforms)
                {
                    graphTransform.Run(builder);
                }
            }

            var result = builder.Build();
            runtimeGraphAsset.GraphDefinition = result.GraphDefinition;
            runtimeGraphAsset.DebugData = builder.CreateDebugData();
            EditorUtility.SetDirty(runtimeGraphAsset);

            if (builder.UnitsToCodegen.Any())
            {
                if ((options & TranslationOptions.CodegenApiNodes) != 0)
                    GenerateNodes();
                else
                {
                    if ((options & TranslationOptions.ForceApiReflectionNodes) == 0)
                    {
                        var nodesToCodegen = string.Join("", builder.UnitsToCodegen.Select(m => m is MemberUnit memberUnit ? $"\n  - {memberUnit.member}" : $"\n  - {m.GetType().Name}"));
                        messages.Add(new Warning(WarningLevel.Important, $"This graph contains nodes that require code generation:{nodesToCodegen}\nClick here to generate and compile them. Otherwise, the slower reflection-based implementation will be used", "Generate", GenerateNodes));
                    }

                    foreach (var node in builder.UnitsToCodegen)
                    {
                        node.Analysis<UnitAnalysis>(flowGraphContext).warnings.Add(new Warning(WarningLevel.Important, "This node requires code generation, otherwise it will use the slower reflected node. Click here to generate the code of this node",
                            "Generate", () =>
                            {
                                CodeGeneratorUtils.GenerateNode(node);
                                AssetDatabase.Refresh();
                            }));
                    }
                }
            }
            else
                messages.Add(new Warning(WarningLevel.Info, $"No code generation for the new interpreter is needed for this graph."));


            runtimeGraphAsset.Hash = runtimeGraphAsset.GraphDefinition.ComputeHash();

            void GenerateNodes()
            {
                foreach (var node in builder.UnitsToCodegen)
                    CodeGeneratorUtils.GenerateNode(node);
                AssetDatabase.Refresh();
            }

            void LogMessage(IUnit unit, WarningLevel level, string s)
            {
                Action selectAction = () => flowGraphContext.selection.Select(unit);
                messages.Add(new Warning(level, s, "Select Node", selectAction));
                Debug.LogFormat(Warning.WarningLevelToLogType(level), LogOption.None, null, s);
            }
        }

        [AnalyserAttribute(typeof(FlowGraph))]
        public class NewRuntimeAnalyzer : Analyser<FlowGraph, NewRuntimeAnalysis>
        {
            public List<Warning> Messages;
            public NewRuntimeAnalyzer(GraphReference reference, FlowGraph target) : base(reference, target)
            {
                // Debug.Log(nameof(NewRuntimeAnalyzer));
            }

            [Assigns]
            protected virtual IEnumerable<Warning> Warnings() => Messages;
        }

        public class NewRuntimeAnalysis : IAnalysis
        {
            public List<Warning> warnings { get; set; } = new List<Warning>();
            public NewRuntimeAnalysis()
            {
            }
        }

        internal static void TranslateEmbeddedConstants(IUnit unit, GraphBuilder builder, PortMapper portMapper)
        {
            // Embedded values
            foreach (ValueInput unitValueInput in unit.valueInputs)
            {
                if (!portMapper.MappedPortModels.ContainsKey(unitValueInput))
                    continue;
                if (unitValueInput.hasDefaultValue && !unitValueInput.hasValidConnection)
                {
                    var value = unit.defaultValues[unitValueInput.key];
                    var embeddedNodeId = builder.GetNextNodeId();
                    if (unitValueInput.nullMeansSelf && value == null)
                    {
                        var selfNode = new ThisNode();
                        PortMapper embeddedMapping = new PortMapper();
                        embeddedMapping.AddSinglePort(builder, null, ref selfNode.Self);
                        builder.AddNodeInternal(embeddedNodeId, selfNode, embeddedMapping);
                        builder.CreateEdge(selfNode.Self, unitValueInput);
                    }
                    else if (TranslateConstant(builder, out var embeddedNode, out var embeddedMapping, unitValueInput.type,
                        value,
                        unitValueInput,
                        out var valuePort))
                    {
                        builder.AddNodeInternal(embeddedNodeId, embeddedNode, embeddedMapping);
                        builder.CreateEdge(valuePort, unitValueInput);
                    }
                }
            }
        }

        [Pure]
        internal static bool GetMatchingAuthoringPort(Dictionary<(string, PortDirection), IUnitPort> authoringPorts, Type runtimeType, string fieldInfoName,
            PortDirection fieldDirection, bool nameComesFromAttribute, FieldInfo fieldInfo, out IUnitPort unitPort)
        {
            if (!authoringPorts.TryGetValue((fieldInfoName, fieldDirection), out unitPort))
            {
                //if the name comes from the port attribute, expect an exact match
                if (nameComesFromAttribute)
                {
                    Debug.LogError(
                        $"No matching authoring port for runtime port {runtimeType}.{fieldInfo.Name}, which has a {nameof(PortDescriptionAttribute)} with the name {fieldInfoName}");
                    return false;
                }

                // otherwise: check for case conflicts (result/Result). if there's none, just take the port. otherwise, require a PortDescription attribute with the exact name
                try
                {
                    var caseInsensitiveMatchingAuthoringPort = authoringPorts.SingleOrDefault(p =>
                        string.Equals(p.Key.Item1, fieldInfoName, StringComparison.InvariantCultureIgnoreCase));
                    if (caseInsensitiveMatchingAuthoringPort.Key.Item1 != null)
                    {
                        fieldInfoName = caseInsensitiveMatchingAuthoringPort.Key.Item1;
                        unitPort = caseInsensitiveMatchingAuthoringPort.Value;
                        return true;
                    }
                }
                catch (InvalidOperationException e)
                {
                    var names = String.Join(", ", authoringPorts.Where(p =>
                        string.Equals(p.Key.Item1, fieldInfoName, StringComparison.InvariantCultureIgnoreCase)));
                    Debug.LogError(
                        $"Multiple authoring ports with the same name but a different case prevents auto-mapping of ports. Add a [PortDescriptionAttribute(\"authoring port key\")] attribute on the runtime field \"{runtimeType}.{fieldInfo.Name}\". Authoring port names found: {names}\nRawException: {e}");
                    return false;
                }

                // TODO codegen reflection runtime nodes with the right %/&port name directly ?
                fieldInfoName = fieldDirection == PortDirection.Input ? $"%{fieldInfoName}" : $"&{fieldInfoName}";
                if (!authoringPorts.TryGetValue((fieldInfoName, fieldDirection), out unitPort))
                {
                    Debug.LogError($"No matching authoring port for runtime port {runtimeType}.{fieldInfo.Name}");
                    return false;
                }
            }

            return true;
        }

        internal static bool TranslateNode(IUnit unit, GraphBuilder builder, out INode node, out PortMapper mapping,
            TranslationOptions options, Action<WarningLevel, string> logMessage = null)
        {
            if (CodeGeneratorUtils.ShouldGenerateCode(unit, options))
                builder.AddUnitToCodegen(unit);

            if (logMessage == null)
                logMessage = (wl, m) => Debug.LogFormat(Warning.WarningLevelToLogType(wl), LogOption.None, null, m);

            if (s_UnitToTranslatorMapping.TryGetValue(unit.GetType(), out var nodeTranslator))
            {
                nodeTranslator.TranslateUnit(builder, unit, out node, out mapping);
                if (node != null)
                    SetEventCoroutineField(node.GetType(), node);

                return false;
            }


            var tryGetValue = GetUnitRuntimeType(unit, out var runtimeType);
            if (!tryGetValue)
            {
                logMessage(WarningLevel.Error, "No runtime type for " + unit.GetType());
                node = null;
                mapping = null;
                return false;
            }

            node = (INode)Activator.CreateInstance(runtimeType);
            SetEventCoroutineField(runtimeType, node);

            mapping = builder.AutoAssignPortIndicesAndMapPorts(unit, node);

            builder.AddNodeFromModel(unit, node, mapping);
            TranslateEmbeddedConstants(unit, builder, mapping);

            return true;

            void SetEventCoroutineField(Type nodeRuntimeType, INode runtimeNode)
            {
                if (unit is IEventUnit eventUnit)
                {
                    var fieldInfo = nodeRuntimeType.GetField("Coroutine");
                    Assert.IsNotNull(fieldInfo, "Event Nodes must have a 'bool Coroutine' field");
                    fieldInfo.SetValue(runtimeNode, eventUnit.coroutine);
                }
            }
        }

        internal static void TranslateInvokeMemberAsDataNodeAndCacheNode(IUnit unit, GraphBuilder builder, INode node,
            InvokeMember invokeMember, out PortMapper nodeMapping)
        {
            // the unit is used as a flow node, but the codegened node is a data node
            // generate the node and a cache node. the cache node is a flow node, so its execution ports will
            // map to the unit's exec ports. the cache value output ports will map to the unit output ports,
            // and the data node's value input ports to the unit input ports. the data node outputs are
            // connected to the cache input ports

            IEnumerable<(IUnitPort unitPort, FieldInfo runtimePort)> ios = GetNodePorts(node.GetType())
                .Select(f => (f.GetAttribute<PortDescriptionAttribute>(), f))
                .Where(x => x.Item1 != null)
                .Select(x =>
                {
                    if (typeof(IInputPort).IsAssignableFrom(x.f.FieldType))
                        return ((IUnitPort)invokeMember.valueInputs[x.Item1.AuthoringPortName], x.f);
                    return (invokeMember.valueOutputs[x.Item1.AuthoringPortName], x.f);
                });
            var inputsToMap = ios.Where(x => x.unitPort is ValueInput)
                .ToDictionary(x => x.unitPort, x => x.runtimePort);

            var outputs = ios.Where(x => x.unitPort is ValueOutput);

            // node input ports are mapped. cache node output ports are mapped
            // node outputs are connected to cache inputs

            // cache node

            var cacheMapping = new PortMapper();

            var cacheNode = new CacheNode();
            cacheNode.CopiedOutputs.DataCount = invokeMember.outputParameters.Count + 1; // return value
            // TODO tf
            cacheNode.CopiedInputs.DataCount = cacheNode.CopiedOutputs.DataCount;

            cacheMapping.AddSinglePort(builder, invokeMember.enter, ref cacheNode.Enter);
            cacheMapping.AddSinglePort(builder, invokeMember.exit, ref cacheNode.Exit);
            // map cache input, no unit port
            cacheMapping.AddMultiPort(builder, null, ref cacheNode.CopiedInputs);
            // map cache output to unit port
            cacheMapping.AddMultiPort(builder, outputs.Select(x => x.unitPort).ToList(), ref cacheNode.CopiedOutputs);

            var cacheNodeId = builder.GetNextNodeId();
            builder.AddNodeInternal(cacheNodeId, cacheNode, cacheMapping, unit);

            // data node + internal edges

            nodeMapping = new PortMapper();
            List<(OutputDataPort, InputDataPort)> internalEdges = new List<(OutputDataPort, InputDataPort)>();

            uint inputIndex = 0;
            foreach (var input in inputsToMap)
            {
                var nodeInput = (InputDataPort)input.Value.GetValue(node);
                nodeMapping.AddSinglePort(builder, input.Key, ref nodeInput);
                input.Value.SetValue(node, nodeInput);
            }

            // if (invokeMember.member.requiresTarget)
            // {
            //     nodeMapping.AddSinglePort(builder, invokeMember.target, ref node);
            // }

            foreach (var output in outputs)
            {
                // map node out, no unit port
                var nodeOutput = (OutputDataPort)output.runtimePort.GetValue(node);
                nodeMapping.AddSinglePort(builder, null, ref nodeOutput);
                output.runtimePort.SetValue(node, nodeOutput);

                var cacheInputPort = cacheNode.CopiedInputs.SelectPort(inputIndex++);
                internalEdges.Add((nodeOutput, cacheInputPort));
            }

            var nextNodeId = builder.GetNextNodeId();
            builder.AddNodeInternal(nextNodeId, node, nodeMapping, unit);

            foreach (var (nodeOutput, cacheInputPort) in internalEdges)
                builder.CreateEdge(nodeOutput, cacheInputPort);
        }

        internal static void InitCache()
        {
            CodeGeneratorUtils.Init();

            if (s_UnitToTranslatorMapping != null)
                return;

            var callbacks = TypeCache.GetTypesDerivedFrom<IGraphTranslationCallbackReceiver>();
            foreach (var cb in callbacks.Select(t => (IGraphTranslationCallbackReceiver)Activator.CreateInstance(t)))
            {
                cb.OnCacheInitialization();
            }

            s_ModelToRuntimeMapping = new Dictionary<Type, Type>();
            s_ModelVariantToRuntimeMapping = new Dictionary<Type, (MemberInfo enumMember, Dictionary<object, Type>)>();
            foreach (var type in TypeCache.GetTypesWithAttribute<NodeDescriptionAttribute>())
            {
                var attr = type.GetAttribute<NodeDescriptionAttribute>();
                if (attr.ModelType != null && !typeof(IUnit).IsAssignableFrom(attr.ModelType))
                    Debug.LogError($"Runtime node type '{type}' has a model type not deriving from IUnit: '{attr.ModelType.FullName}'");
                else
                {
                    if (attr.SpecializationOf != null)
                    {
                        Assert.IsTrue(attr.SpecializationOf.GetType().IsEnum);
                        if (!s_ModelVariantToRuntimeMapping.TryGetValue(attr.ModelType, out var variants))
                        {
                            var attrs = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
                            MemberInfo member = (MemberInfo)attr.ModelType.GetFields(attrs).FirstOrDefault(f => f.FieldType == attr.SpecializationOf.GetType())
                                ?? attr.ModelType.GetProperties(attrs).FirstOrDefault(f => f.PropertyType == attr.SpecializationOf.GetType());
                            Assert.IsNotNull(member, $"Could not find a member on unit type {attr.ModelType} of variant type {attr.SpecializationOf.GetType()}");
                            s_ModelVariantToRuntimeMapping.Add(attr.ModelType, variants = (member, new Dictionary<object, Type>()));
                        }
                        variants.Item2.Add(attr.SpecializationOf, type);
                    }
                    else
                        s_ModelToRuntimeMapping.Add(attr.ModelType, type);
                }
            }

            s_UnitToTranslatorMapping = TypeCache.GetTypesDerivedFrom(typeof(INodeTranslator))
                .Where(t => !t.IsAbstract)
                .Select(t => (INodeTranslator)Activator.CreateInstance(t))
                .ToDictionary(t => t.TranslatedUnitType);
        }

        private static bool GetUnitRuntimeType(IUnit unit, out Type runtimeType)
        {
            if (s_ModelVariantToRuntimeMapping.TryGetValue(unit.GetType(), out var variants))
            {
                object variantKey;
                if (variants.enumMember is FieldInfo fieldInfo)
                    variantKey = fieldInfo.GetValue(unit);
                else
                    variantKey = ((PropertyInfo)variants.enumMember).GetValue(unit, null);
                return variants.Item2.TryGetValue(variantKey, out runtimeType);
            }
            return s_ModelToRuntimeMapping.TryGetValue(unit.GetType(), out runtimeType);
        }

        public static bool TranslateConstant(GraphBuilder builder, out INode node, out PortMapper portToOffsetMapping,
            Type valueType, object value, IUnitPort literalOutputPort, out OutputDataPort valuePort)
        {
            if (valueType.IsEnum)
            {
                var cf = new ConstantEnum { Value = (Enum)value };
                portToOffsetMapping = new PortMapper();
                portToOffsetMapping.AddSinglePort(builder, literalOutputPort, ref cf.Output);
                valuePort = cf.Output;
                node = cf;
                return true;
            }

            var searchedType = typeof(Object).IsAssignableFrom(valueType) ? typeof(Object) : valueType;
            if (!GraphTranslationCallbackReceiver.LiteralModelToRuntimeMapping.TryGetValue(searchedType,
                out Type runtimeType))
            {
                Debug.LogError("No runtime literal type for literal " + valueType);
                node = null;
                portToOffsetMapping = null;
                valuePort = default;
                return false;
            }

            if (GraphTranslationCallbackReceiver.CustomConstantBuilders.TryGetValue(searchedType,
                out var constantBuilder))
            {
                node = constantBuilder.Build(value);
            }
            else
            {
                node = (INode)Activator.CreateInstance(runtimeType);
                runtimeType.GetField("Value").SetValue(node, value);
            }

            portToOffsetMapping = new PortMapper();
            var outputField = runtimeType.GetField(nameof(ConstantFloat.Output));
            valuePort = (OutputDataPort)outputField.GetValue(node);
            portToOffsetMapping.AddSinglePort(builder, literalOutputPort, ref valuePort);
            outputField.SetValue(node, valuePort);
            return true;
        }

        public static IEnumerable<(FieldInfo, IOutputDataPort)> GetNodeOutputPorts(INode n)
        {
            foreach (var portField in GetNodePorts(n.GetType()))
            {
                if (portField.GetValue(n) is IOutputDataPort port)
                    yield return (portField, port);
            }
        }

        public static IEnumerable<(FieldInfo, IInputDataPort)> GetNodeInputPorts(INode n)
        {
            foreach (var portField in GetNodePorts(n.GetType()))
            {
                if (portField.GetValue(n) is IInputDataPort port)
                    yield return (portField, port);
            }
        }

        public static IEnumerable<FieldInfo> GetNodePorts(Type t)
        {
            return t.GetFields().Where(f => typeof(IPort).IsAssignableFrom(f.FieldType));
        }
    }
}
