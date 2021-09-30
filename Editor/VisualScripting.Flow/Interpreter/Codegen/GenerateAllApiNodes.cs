using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.VisualScripting.Interpreter
{
    public static class GenerateAllApiNodes
    {
        private static bool _cancelled;


        [MenuItem("internal:Visual Scripting/Cancel")]
        public static void Cancel() => _cancelled = true;

        [MenuItem("internal:Visual Scripting/Generate all reflected units")]
        public static void GenerateAll()
        {
            UnitOptionTree tree = new UnitOptionTree(new GUIContent("asd"));
            GameObject go = new GameObject();
            ScriptMachine flowMachine = go.AddComponent<ScriptMachine>();
            flowMachine.nest.embed = flowMachine.DefaultGraph();
            flowMachine.nest.source = GraphSource.Embed;
            tree.reference = GraphReference.New(flowMachine, Enumerable.Empty<Guid>(), false);
            tree.Prewarm();
            _cancelled = false;
            new Thread(() =>
            {
                int readCount = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();

                using (var f = File.Open("list.txt", FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(f))
                {
                    foreach (object childValue in tree.Root())
                    {
                        if (_cancelled)
                            break;
                        Rec(null, tree, childValue, sw, 0, ref readCount);
                    }

                    sw.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
                }


                _cancelled = true;
            }).Start();
            EditorApplication.update += DoUpdate;
        }

        private static void DoUpdate()
        {
            if (_cancelled)
            {
                Debug.Log("done");
                EditorApplication.update -= DoUpdate;
            }
        }

        private static void Rec(FuzzyOptionNode node, UnitOptionTree tree, object childValue, StreamWriter sb,
            int indent, ref int readCount)
        {
            if (_cancelled)
                return;
            IFuzzyOption childOption = tree.Option(childValue);

            string optionLabel;
            bool dump = true;
            if (childOption is IMemberUnitOption memberOption)
            {
                optionLabel = "* " + memberOption.member;
                dump = !memberOption.member.isInherited;
                if (dump)
                    CodeGeneratorUtils.GenerateNode((MemberUnit)memberOption.unit);
            }
            else
                optionLabel = childOption.label;

            if (dump)
            {
                sb.WriteLine($"{new string(' ', indent * 2)}{optionLabel}");
                if (readCount++ % 100 == 0)
                    sb.Flush();
            }

            try
            {
                childOption.OnPopulate();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to display {childOption.GetType()}: \n{ex}");
                return;
            }

            var enumerable = tree.Children(childValue);
            bool hasChildren = enumerable.Any();

            bool include = !childOption.parentOnly || hasChildren;

            if (!include)
            {
                return;
            }

            string label = childOption.label;

            FuzzyOptionNode childNode = new FuzzyOptionNode(childOption, label);

            childNode.hasChildren = hasChildren;

            node?.children?.Add(childNode);

            if (hasChildren)
                foreach (object subchild in enumerable)
                {
                    Rec(childNode, tree, subchild, sb, indent + 1, ref readCount);
                }
        }
    }
}
