using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    public static class InternalNodeCodeGenerator
    {
        const string MultiInputPort = "Inputs";
        const string Result = "result";
        static Dictionary<string, string> HookValuesToFields;

        private static string DevGeneratedNodesFolder =>
            Path.Combine(PathUtility.GetPackageRootPath(),
                "Runtime/VisualScripting.Flow/Interpreter/Nodes/Generated");

        /// <summary>
        /// Internal: codegen event nodes. generated files contain regions meant to be edited by devs and that will be preserved when re-generating the code
        /// </summary>
        [MenuItem("internal:Visual Scripting/Generate runtime event nodes",
            priority = LudiqProduct.DeveloperToolsMenuPriority + 1001)]
        internal static void GenerateEventNodes()
        {
            var folder = Path.Combine(DevGeneratedNodesFolder, "Events");

            List<(Type argType, Type unitType)> eventArgTypes = new List<(Type argType, Type unitType)>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<IEventUnit>())
            {
                if (type.IsAbstract || type.Assembly != typeof(ScalarSum).Assembly ||
                    Attribute.IsDefined(type, typeof(ObsoleteAttribute)))
                    continue;
                if (type == typeof(CustomEvent))
                    continue;
                GenerateEventNode(folder, type, out var eventArgType);
                eventArgTypes.Add((eventArgType, type));
            }

            StringBuilder sb = new StringBuilder();
            foreach (var eventArgType in eventArgTypes.ToLookup(x => x.argType, x => x.unitType))
            {
                sb.AppendLine($@"{eventArgType.Key}: {string.Join(", ", eventArgType.Select(x => x.Name))}");
                foreach (var unitType in eventArgType)
                {
                    sb.AppendLine(
                        $"  {unitType.Name}: {string.Join("", unitType.GetProperties().Where(p => p.PropertyType == typeof(ValueOutput)).Select(p => $"{Environment.NewLine}    {p.Name}"))}");
                }
            }

            Debug.Log(sb.ToString());
            File.WriteAllText("events.yaml", sb.ToString());

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Internal: generates an event node from an event unit type
        /// </summary>
        /// <param name="folder">Target folder for code</param>
        /// <param name="unitType">The source unit for the codegen</param>
        /// <param name="eventArgTypes">The unit arg type, eg. EmptyEventArgs, for debugging purposes</param>
        private static void GenerateEventNode(string folder, Type unitType, out Type eventArgTypes)
        {
            Assert.IsNotNull(unitType);
            string generatedNodeName = unitType.Name + "Node";

            var subfolder = unitType.GetCustomAttribute<UnitCategory>()?.name;
            var fileFolderPath = subfolder == null ? folder : Path.Combine(folder, subfolder);
            Directory.CreateDirectory(fileFolderPath);

            var filePath = Path.Combine(fileFolderPath, generatedNodeName + ".cs");
            var handWrittenCodeRegions = ExtractHandWrittenCodeRegions(filePath);

            eventArgTypes = GetArgTypes();
            string hookName = HookName(out var messageListenerType);

            string ports = "";
            CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Trigger, "Trigger");

            string targetGameObjectPort = null;
            foreach (var propertyInfo in unitType.GetProperties().Where(p => p.PropertyType == typeof(ValueInput)))
            {
                string portDescriptionName = null;
                if (typeof(IGameObjectEventUnit).IsAssignableFrom(unitType) &&
                    propertyInfo.Name == nameof(GameObjectEventUnit<EmptyEventArgs>.target))
                {
                    targetGameObjectPort = CodeGeneratorUtils.ToCamelCase(propertyInfo.Name);
                    portDescriptionName = nameof(GameObjectEventUnit<EmptyEventArgs>.target);
                }

                CodeGeneratorUtils.AddPort(ref ports, PortDirection.Input, PortType.Data,
                    CodeGeneratorUtils.ToCamelCase(propertyInfo.Name), portDescriptionName: portDescriptionName);
            }

            if (eventArgTypes != typeof(EmptyEventArgs))
            {
                foreach (var propertyInfo in unitType.GetProperties().Where(p => p.PropertyType == typeof(ValueOutput)))
                {
                    CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Data,
                        CodeGeneratorUtils.ToCamelCase(propertyInfo.Name));
                }
            }

            var messageListenerTypeProp =
                messageListenerType != null ? $" typeof({messageListenerType.CSharpFullName()})" : "null";
            string body = $@"        public bool Coroutine;
        public Type MessageListenerType => {messageListenerTypeProp};

        public Execution Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {{
            ctx.Trigger(Trigger, Coroutine);
            return Execution.Done;
        }}

        public void Register<TCtx>(TCtx ctx, NodeId nodeId) where TCtx : IGraphInstance
        {{
            ctx.RegisterEventHandler<{eventArgTypes.CSharpFullName()}>(nodeId, this, {hookName}, {targetGameObjectPort ?? "default"});
        }}

        public void AssignArguments<TCtx>(TCtx ctx, {eventArgTypes.CSharpFullName()} args) where TCtx : IGraphInstance
        {{
{(eventArgTypes != typeof(EmptyEventArgs) ? CodeGeneratorUtils.HandWrittenRegion("assign", handWrittenCodeRegions, "// TODO assign arguments" + Environment.NewLine) : null)}
        }}";


            var code = CodeGeneratorUtils.TemplateNode(
                generatedNodeName,
                typeof(IEntryPointRegisteredNode<>).MakeGenericType(eventArgTypes),
                ports,
                body,
                NodeDescriptionAttribute.ToString(unitType)
            );
            Debug.Log(code);
            File.WriteAllText(filePath, code);

            Type GetArgTypes()
            {
                var argTypes = typeof(EmptyEventArgs);

                var t = unitType;
                while (t != null)
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(EventUnit<>))
                    {
                        argTypes = t.GetGenericArguments()[0];
                        break;
                    }

                    t = t.BaseType;
                }

                return argTypes;
            }

            string HookName(out Type messageListenerType2)
            {
                if (HookValuesToFields == null)
                {
                    HookValuesToFields = new Dictionary<string, string>();
                    foreach (var fieldInfo in typeof(EventHooks).GetFields())
                    {
                        var value = (string)fieldInfo.GetValue(null);
                        var fieldName = $"EventHooks.{fieldInfo.Name}";
                        HookValuesToFields.Add(value, fieldName);
                    }
                }

                var unit = Activator.CreateInstance(unitType) as IEventUnit;

                messageListenerType2 = null;
                if (unit is IGameObjectEventUnit gameObjectEventUnit)
                    messageListenerType2 = gameObjectEventUnit.MessageListenerType;

                var hookNameField = unitType.GetProperty("hookName",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (hookNameField != null)
                {
                    var hookNameValue = hookNameField.GetValue(unit, null) as string;
                    Assert.IsNotNull(hookNameValue, nameof(hookNameValue) + " != null");
                    if (HookValuesToFields.TryGetValue(hookNameValue, out var varName))
                        return varName;
                }

                return "EventHooks.Update";
            }
        }

        private static Dictionary<string, string> ExtractHandWrittenCodeRegions(string filePath)
        {
            Dictionary<string, string> handWrittenCodeRegions = null;
            if (File.Exists(filePath))
            {
                var existingCode = File.ReadAllLines(filePath);
                handWrittenCodeRegions = existingCode
                    .Select((line, lineIndex) => (lineIndex, start: CodeGeneratorUtils.RegionStartRegex.Match(line),
                        end: CodeGeneratorUtils.RegionEndRegex.Match(line)))
                    .Where(x => x.start.Success || x.end.Success)
                    .GroupBy(x => (x.start.Success ? x.start : x.end).Groups["name"].Value)
                    .ToDictionary(x => x.Key, x =>
                    {
                        Assert.AreEqual(2, x.Count(), "Found more than one start and one end for region " + x.Key);
                        var start = x.First(y => y.start.Success).lineIndex;
                        var end = x.First(y => y.end.Success).lineIndex;
                        return string.Join("",
                            existingCode.Skip(start + 1).Take(end - start - 1).Select(l => l + Environment.NewLine));
                    });
            }

            return handWrittenCodeRegions;
        }

        private static StringBuilder s_TestsFile;
        private static Dictionary<string, string> s_TestRegions;

        /// <summary>
        /// Internal: codegen all "simple" data nodes. generated files contain regions meant to be edited by devs and that will be preserved when re-generating the code
        /// </summary>
        [MenuItem("internal:Visual Scripting/Generate runtime data nodes",
            priority = LudiqProduct.DeveloperToolsMenuPriority + 1001)]
        internal static void GenerateDataNodes()
        {
            var testFile = "Packages/com.unity.visualscripting/Tests/Editor/Interpreter/Graphs/DataNodesTests.generated.cs";
            s_TestsFile = new StringBuilder();
            s_TestRegions = ExtractHandWrittenCodeRegions(testFile);

            var folder = DevGeneratedNodesFolder;

            GenerateDataNode<And, bool>(folder, "&&");
            GenerateDataNode<Or, bool>(folder, "||");
            GenerateDataNode<ExclusiveOr, bool>(folder, "^");
            GenerateDataNode<Negate, bool>(folder, "!");

            GenerateDataNode<Equal, Value>(folder, "Value.Equals", customTranslation: true);
            GenerateDataNode<NotEqual, Value>(folder, "!Value.Equals", customTranslation: true);
            GenerateDataNode<Less, Value>(folder, "<");
            GenerateDataNode<LessOrEqual, Value>(folder, "<=");
            GenerateDataNode<Greater, Value>(folder, ">");
            GenerateDataNode<GreaterOrEqual, Value>(folder, ">=");

            GenerateDataNode<ScalarExponentiate, float>(folder, "Mathf.Pow");

            GenerateDataNode<Vector3CrossProduct, Vector3>(folder, "Vector3.Cross");
            GenerateDataNode<Vector2Angle, Vector2, float>(folder, "Vector2.Angle");
            GenerateDataNode<Vector3Angle, Vector3, float>(folder, "Vector3.Angle");
            GenerateDataNode<Vector3Project, Vector3>(folder, "Vector3.Project");

            foreach (var mathType in new[]
                 {
                     ("Scalar", typeof(float), 1),
                     ("Vector2", typeof(Vector2), 2),
                     ("Vector3", typeof(Vector3), 3),
                     ("Vector4", typeof(Vector4), 4),
                 })
            {
                var boltNamespace = typeof(ScalarSum).Namespace;

                Type T(string s)
                {
                    var name = $"{boltNamespace}.{mathType.Item1}{s}";
                    return typeof(ScalarSum).Assembly.GetType(name);
                }

                GenerateDataNode(folder, "-", T("Subtract"), new[] { mathType.Item2 });

                GenerateDataNode(folder, "/", T("Divide"), new[] { mathType.Item2 },
                    componentCountIfComponentWise: mathType.Item3);
                GenerateDataNode(folder, "%", T("Modulo"), new[] { mathType.Item2 },
                    componentCountIfComponentWise: mathType.Item3);
                GenerateDataNode(folder, "*", T("Multiply"), new[] { mathType.Item2 },
                    componentCountIfComponentWise: mathType.Item3);

                GenerateDataNode(folder, "Mathf.Abs", T("Absolute"), new[] { mathType.Item2 },
                    componentCountIfComponentWise: mathType.Item3);

                // Mathf.<op> for floats, Vector<N>.<op> for vectors
                var prefix = mathType.Item2 == typeof(float) ? "Mathf" : mathType.Item1;

                GenerateDataNode(folder, null, T("PerSecond"), new[] { mathType.Item2 }, resultOp: " * ctx.TimeData.DeltaTime", isFoldable: false);

                // lerps take a float as the T param
                GenerateDataNode(folder, $"{prefix}.Lerp", T("Lerp"), new[]
                {
                    mathType.Item2, mathType.Item2,
                    typeof(float)
                });

                GenerateDataNode(folder, $"{prefix}.Min", T("Minimum"), new[] { mathType.Item2 });
                GenerateDataNode(folder, $"{prefix}.Max", T("Maximum"), new[] { mathType.Item2 });
                GenerateDataNode(folder, $"{prefix}.MoveTowards", T("MoveTowards"),
                    new[] { mathType.Item2, mathType.Item2, typeof(float) });
                GenerateDataNode(folder, $"+", T("Sum"), new[] { mathType.Item2 });
                // div by input count for averages
                GenerateDataNode(folder, $"+", T("Average"), new[] { mathType.Item2 }, resultOp: $" / {MultiInputPort}.DataCount");

                if (mathType.Item3 == 1)
                    continue;
                // Vector only nodes
                GenerateDataNode(folder, $"{prefix}.Distance", T("Distance"), new[] { mathType.Item2 }, typeof(float));
                GenerateDataNode(folder, $"{prefix}.Dot", T("DotProduct"), new[] { mathType.Item2 }, typeof(float));
                GenerateDataNode(folder, null, T("Normalize"), new[] { mathType.Item2 }, resultOp: ".normalized");
            }

            var testCode = $@"using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Unity.VisualScripting.Interpreter;
using Unity.VisualScripting.Tests;

namespace Unity.VisualScripting.Tests
{{
    class DataNodesTestsGenerated : RuntimeNodeTestBase
    {{
{s_TestsFile}
    }}
}}
";
            Debug.Log(testCode);
            File.WriteAllText(testFile, testCode);
            s_TestsFile = null;
            s_TestRegions = null;
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Internal: Generates a data node
        /// </summary>
        /// <param name="folder">Destination folder for the generated code</param>
        /// <param name="op">The operation used to combine inputs. can be an operator like "+" or a method call like "Mathf.Abs"</param>
        /// <param name="unitType">The source unit type</param>
        /// <param name="valueType">The types of input ports to determine the right Read{Type}. if it's length is 1, use its only value for all ports</param>
        /// <param name="resultOp">Operation applied after the op computation, eg. ".normalized" or "* Time.deltaTime"</param>
        /// <param name="componentCountIfComponentWise">apply op to each component of a vector, eg. result = new Vector2(a.x+b.x, a.y+b.y)</param>
        private static void GenerateDataNode(string folder, string op, Type unitType, Type[] valueType = null, Type outputType = null,
            string resultOp = null, int componentCountIfComponentWise = 0, bool isFoldable = true)
        {
            Assert.IsNotNull(unitType, op);

            bool isInfix = op == null || !op.Any(char.IsLetter);
            bool isMultiInputNode = typeof(IMultiInputUnit).IsAssignableFrom(unitType);
            bool isComponentWise = componentCountIfComponentWise > 1;
            Assert.IsFalse(isMultiInputNode && isComponentWise,
                "isComponentWise and multiInputNodes not supported at the same time");

            var subfolder = unitType.GetCustomAttribute<UnitCategory>()?.name;
            var filePath = subfolder == null ? folder : Path.Combine(folder, subfolder);
            Directory.CreateDirectory(filePath);

            string generatedNodeName = unitType.Name + "Node";
            var runtimeNodeInterface = typeof(IDataNode);

            var portInfo = new Dictionary<string, (PortType portType, PortDirection portDirection, bool ismulti, Type dataType)>();

            string outputPort = null;
            string ports = "";
            List<string> inputPorts = new List<string>();

            if (isMultiInputNode)
            {
                CodeGeneratorUtils.AddPort(ref ports, PortDirection.Input, PortType.Data, MultiInputPort, isMultiPort: true);
                portInfo.Add(MultiInputPort, (PortType.Data, PortDirection.Input, true, GetPortType(0)));
            }

            foreach (var property in unitType.GetProperties())
            {
                var propertyName = CodeGeneratorUtils.ToCamelCase(property.Name);
                if (!isMultiInputNode)
                {
                    if (typeof(ValueInput) == property.PropertyType)
                    {
                        CodeGeneratorUtils.AddPort(ref ports, PortDirection.Input, PortType.Data, propertyName);
                        portInfo.Add(propertyName, (PortType.Data, PortDirection.Input, false, GetPortType(inputPorts.Count)));
                        inputPorts.Add(propertyName);
                        continue;
                    }
                }

                if (typeof(ValueOutput) == property.PropertyType)
                {
                    Assert.IsNull(outputPort);
                    outputPort = propertyName;
                    CodeGeneratorUtils.AddPort(ref ports, PortDirection.Output, PortType.Data, propertyName, portDescriptionName: property.GetAttribute<PortKeyAttribute>()?.key);
                    portInfo.Add(propertyName, (PortType.Data, PortDirection.Output, false, outputType ?? GetPortType(0)));
                }
            }

            Assert.IsNotNull(outputPort);

            string compute = "";
            if (isMultiInputNode)
            {
                string nextInput = $"{CodeGeneratorUtils.GetReadCall(GetPortType(0), $"{MultiInputPort}.SelectPort(i)", false)}";
                string combine;
                if (isInfix)
                    combine = $"{Result} {op} {nextInput};";
                else
                    combine = $"{op}({Result}, {nextInput});";
                compute = $@"var result = {CodeGeneratorUtils.GetReadCall(GetPortType(0), $"{MultiInputPort}.SelectPort(0)", false)};
            for (uint i = 1; i < {MultiInputPort}.DataCount; i++)
                {Result} = {combine}
            ctx.Write({outputPort}, {Result}{resultOp});";
            }
            else // might be component wise
            {
                string operation;
                if (!isComponentWise)
                {
                    operation = MakeOperation(ReadCallIndex);
                }
                else
                {
                    operation = string.Join(", ",
                        new[] { ".x", ".y", ".z", ".w" }.Take(componentCountIfComponentWise).Select(suffix =>
                              MakeOperation(i => inputPorts[i].ToLowerInvariant() + suffix)));
                }

                if (isComponentWise)
                {
                    operation = $"new Vector{componentCountIfComponentWise}({operation})";
                    compute = string.Join("",
                        inputPorts.Select((p, i) =>
                            $"var {p.ToLowerInvariant()} = {ReadCallIndex(i)};{Environment.NewLine}            "));
                }

                compute += $"ctx.Write({outputPort}, {operation}{resultOp});";
            }

            string body = $@"        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {{
            {compute}
        }}";

            var code = CodeGeneratorUtils.TemplateNode(
                generatedNodeName,
                isFoldable ? new[] { runtimeNodeInterface, typeof(IFoldableNode) } : new[] { runtimeNodeInterface },
                ports,
                body,
                NodeDescriptionAttribute.ToString(unitType));
            File.WriteAllText(Path.Combine(filePath, generatedNodeName + ".cs"), code);

            GenerateNodeTest(generatedNodeName, portInfo);

            string ReadCallIndex(int i) => CodeGeneratorUtils.GetReadCall(GetPortType(i), inputPorts[i], false);
            Type GetPortType(int i) => valueType[valueType.Length == 1 ? 0 : i];

            string MakeOperation(Func<int, string> getPortExpression)
            {
                string operation;
                if (isInfix)
                    operation = inputPorts.Count == 1
                        ? $"{op}{getPortExpression(0)}" // -a
                        : $"{getPortExpression(0)} {op} {getPortExpression(1)}";
                else // F(a,b,...)
                {
                    var args = inputPorts.Select((p, i) => getPortExpression(i));
                    operation = $"{op}({string.Join(", ", args)})";
                }

                return operation;
            }
        }

        private static void GenerateNodeTest(string generatedNodeName, Dictionary<string, (PortType portType, PortDirection portDirection, bool ismulti, Type dataType)> portInfo)
        {
            string AssignPort(
                KeyValuePair<string, (PortType portType, PortDirection portDirection, bool ismulti, Type dataType)> p)
            {
                var paramName = $"{p.Value.portDirection.ToString().ToLowerInvariant()}{p.Key}";
                if (p.Value.portDirection == PortDirection.Input)
                    return $"Const{(p.Value.ismulti ? "s" : "")}({paramName})";

                return $"Expected({paramName})";
            }

            s_TestsFile.AppendLine($"        // {generatedNodeName}");
            var portAssignments = portInfo.Select(x => $"{x.Key} = {AssignPort(x)}");
            var testParams = portInfo.Select(x =>
                $"{x.Value.dataType.CSharpName()}{(x.Value.ismulti ? "[]" : "")} {x.Value.portDirection.ToString().ToLowerInvariant()}{x.Key}");
            var defaultTestCase = $@"             yield break; // TODO write cases
             // yield return new TestCaseData({string.Join(", ", portInfo.Select(x =>
            {
                if (x.Value.dataType == typeof(float))
                    return "0f";
                if (x.Value.dataType == typeof(bool))
                    return "false";
                return $"new {x.Value.dataType.CSharpName()}()";
            }))});
";
            s_TestsFile.AppendLine($@"        static IEnumerable<TestCaseData> {generatedNodeName}Data()
        {{
{CodeGeneratorUtils.HandWrittenRegion(generatedNodeName, s_TestRegions, defaultTestCase)}
        }}

        [Test, TestCaseSource(nameof({generatedNodeName}Data))]
        public void Test{generatedNodeName}({string.Join(", ", testParams)}) =>
            TestNode(new {generatedNodeName} {{{string.Join(",\n                ", portAssignments)}
            }});

");
        }

        private static void GenerateDataNode<T, TInput>(string folder, string op, string resultOp = null,
            bool customTranslation = false) => GenerateDataNode(folder, op, typeof(T), new[] { typeof(TInput) }, resultOp: resultOp);
        private static void GenerateDataNode<T, TInput, TOutput>(string folder, string op, string resultOp = null,
            bool customTranslation = false) => GenerateDataNode(folder, op, typeof(T), new[] { typeof(TInput) }, typeof(TOutput), resultOp);
    }
}
