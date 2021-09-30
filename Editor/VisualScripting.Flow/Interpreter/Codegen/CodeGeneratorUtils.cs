using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using NUnit;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    public static class CodeGeneratorUtils
    {
        internal static readonly Regex RegionStartRegex = new Regex("regionStart='(?<name>[^']*)'", RegexOptions.Compiled);
        internal static readonly Regex RegionEndRegex = new Regex("regionEnd='(?<name>[^']*)'", RegexOptions.Compiled);

        [MenuItem("internal:Visual Scripting/Force generate missing runtime nodes",
            priority = LudiqProduct.DeveloperToolsMenuPriority + 1001)]
        internal static void ForceGenerateMissingNodes() => GenerateMissingMemberNodes(null, true);

        [MenuItem("internal:Visual Scripting/Generate missing runtime nodes",
            priority = LudiqProduct.DeveloperToolsMenuPriority + 1001)]
        internal static void GenerateMissingNodes() => GenerateMissingMemberNodes(null, false);

        static Dictionary<Type, INodeCodeGenerator> s_CodeGenerators;

        internal static void Init()
        {
            if (s_CodeGenerators != null)
                return;

            s_CodeGenerators = TypeCache.GetTypesDerivedFrom<INodeCodeGenerator>()
                .Where(t => !t.IsAbstract)
                .Select(t => (INodeCodeGenerator)Activator.CreateInstance(t))
                .ToDictionary(t => t.UnitType);
        }

        internal static bool ShouldGenerateCode(IUnit unit, TranslationOptions options)
        {
            if (s_CodeGenerators == null)
                return false;

            return s_CodeGenerators.TryGetValue(unit.GetType(), out var generator) && generator.ShouldGenerateCode(unit, options);
        }

        // TODO Update that
        static void GenerateMissingMemberNodes(FlowGraphContext context, bool force)
        {
            var units = new List<IUnit>();

            foreach (var scriptGraphAsset in AssetUtility.GetAllAssetsOfType<ScriptGraphAsset>())//Resources.FindObjectsOfTypeAll<ScriptGraphAsset>())
            {
                foreach (var unit in scriptGraphAsset.graph.units.OfType<MemberUnit>())
                {
                    if (unit.member.ToUniqueString() == "UnityEngine.Debug.Log(System.Object)")
                        continue;
                    if (force)
                        units.Add(unit);
                    else
                    {
                        GraphBuilder builder = new GraphBuilder(context);
                        try
                        {
                            FlowGraphTranslator.TranslateNode(
                                unit,
                                builder,
                                out _,
                                out _,
                                TranslationOptions.TranslateUnusedNodes | TranslationOptions.ForceApiReflectionNodes);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                        units.AddRange(builder.UnitsToCodegen);
                    }
                }
            }

            foreach (var unit in units)
            {
                GenerateNode(unit);
            }
        }

        internal static string GenerateNode(IUnit unit, string folder = null)
        {
            if (!PluginContainer.initialized)
                PluginContainer.Initialize();
            Directory.CreateDirectory(BoltFlow.Paths.generatedNodes);

            if (!s_CodeGenerators.TryGetValue(unit.GetType(), out var generator))
                return null;

            if (!generator.GenerateCode(unit, out string typeName, out string code))
                return null;

            var path = Path.Combine(folder ?? BoltFlow.Paths.generatedNodes, $"{typeName}.generated.cs");
            // TODO Cleanup
            Debug.Log($"{unit.GetType().Name} {unit} @ {path}:\n{code}");
            File.WriteAllText(path, code);
            return path;
        }

        internal static string GetGenerateNodeAsString(IUnit unit)
        {
            if (!PluginContainer.initialized)
                PluginContainer.Initialize();

            if (!s_CodeGenerators.TryGetValue(unit.GetType(), out var generator))
                return null;

            if (!generator.GenerateCode(unit, out string typeName, out string code))
                return null;

            return code;
        }

        private static readonly HashSet<Type> ValueNativeTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
        };
        internal static string WrapValue(Type t, string valueStr) =>
            t.IsClass || t == typeof(string) || (t.IsValueType && !t.IsPrimitive && !t.IsEnum && !ValueNativeTypes.Contains(t)) ? $"Interpreter.Value.FromObject({valueStr})" : valueStr;

        internal static string CallTarget(ref string ports, Member member)
        {
            string call;
            if (member.requiresTarget)
            {
                AddPort(ref ports, PortDirection.Input, PortType.Data, "Target", portDescriptionName: "target");
                call = GetReadCall(member.targetType, "Target", false);
            }
            else
                call = $"{member.targetTypeName}";

            return call;
        }

        internal static string HandWrittenRegion(string regionName, Dictionary<string, string> existingRegions, string defaultRegionContent)
        {
            return $@"// regionStart='{regionName}'
{(existingRegions == null ? defaultRegionContent : existingRegions.TryGetValue(regionName, out var previousCode) && !string.IsNullOrWhiteSpace(previousCode.Replace(Env.NewLine, null)) ? previousCode : defaultRegionContent)}// regionEnd='{regionName}'";
        }

        internal static string TemplateNode(string generatedNodeName, Type runtimeNodeInterface, string ports,
            string body, params string[] attributes) => TemplateNode(generatedNodeName, new[] { runtimeNodeInterface },
            ports, body, attributes);
        internal static string TemplateNode(string generatedNodeName, Type[] runtimeNodeInterfaces, string ports,
            string body, params string[] attributes)
        {
            var attrs = attributes.Aggregate(string.Empty, (current, attribute) => current + $"\n    {attribute}");
            var cSharpFullNames = string.Join(", ", runtimeNodeInterfaces.Select(runtimeNodeInterface => runtimeNodeInterface.CSharpFullName().Replace("Unity.VisualScripting.Interpreter.", null).Replace("Unity.VisualScripting.", null).Replace("VisualScripting.Interpreter.", null)));
            return $@"// GENERATED FILE, do not edit by hand
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{{{attrs}
    public struct {generatedNodeName} : {cSharpFullNames}
    {{
{ports}
{body}
    }}
}}
";
        }

        internal static string ToCamelCase(string name)
        {
            return Char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        internal static string GetFriendlyTypeName(Type type)
        {
            using (var p = new CSharpCodeProvider())
            {
                var r = new CodeTypeReference(type);
                return p.GetTypeOutput(r);
            }
        }

        internal static void AddPort(ref string ports, PortDirection direction, PortType type, string name,
            bool isMultiPort = false, string comment = null, string portDescriptionName = null)
        {
            if (portDescriptionName != null)
                ports += $"        [PortDescription(\"{portDescriptionName}\")]{Environment.NewLine}";
            ports += $"        public {direction}{type}{(isMultiPort ? "Multi" : "")}Port {name};{comment}{Environment.NewLine}";
        }

        internal static string GetReadCall(Type portType, string portName, bool convertToValue)
        {
            var readCall = $"ctx.Read{GetReadCallType()}({portName})";
            if (portType.IsEnum)
                readCall = $"({portType.FullName}) {readCall}.{nameof(Value.EnumValue)}";
            if (convertToValue)
                return WrapValue(portType, readCall);
            return readCall;

            string GetReadCallType()
            {
                switch (portType)
                {
                    case Type t when t.IsClass:
                        return $"Object<{GetFriendlyTypeName(t)}>";
                    case Type t when t == typeof(bool):
                        return "Bool";
                    case Type t when t == typeof(Color):
                        return "Color";
                    case Type t when t == typeof(int):
                        return "Int";
                    case Type t when t == typeof(float):
                        return "Float";
                    case Type t when t == typeof(Vector2):
                        return "Vector2";
                    case Type t when t == typeof(Vector3):
                        return "Vector3";
                    case Type t when t == typeof(Vector4):
                        return "Vector4";
                    case Type t when t == typeof(Quaternion):
                        return "Quaternion";
                    case Type t when t.IsEnum:
                        return $"Value";
                    case Type t when t.IsValueType && t != typeof(Value):
                        return $"Struct<{GetFriendlyTypeName(t)}>";
                    default:
                        return "Value";
                }
            }
        }
    }
}
