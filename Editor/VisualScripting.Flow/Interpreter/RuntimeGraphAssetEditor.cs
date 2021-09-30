using System;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    [CustomEditor(typeof(RuntimeGraphAsset))]
    class RuntimeGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (RuntimeGraphAsset)target;
            var def = asset.GraphDefinition;
            EditorGUILayout.LabelField("Hash", asset.Hash.ToString());
            for (var index = 0; index < def.NodeTable.Length; index++)
            {
                NodeId nodeId = new NodeId((uint)index);
                INode node = def.NodeTable[index];
                if (node == null)
                {
                    EditorGUILayout.LabelField("Unknown node");
                    continue;
                }
                var nodeType = node.GetType();
                var nodeTitle = $"{index + 1} {nodeType.Name}";
                if (node is IConstantNode constantNode)
                    nodeTitle += " " + constantNode.GetType().GetField("Value")?.GetValue(node);

                if (EditorGUILayout.Foldout(true, nodeTitle))
                {
                    EditorGUI.indentLevel++;
                    foreach (var fieldInfo in FlowGraphTranslator.GetNodePorts(nodeType))
                    {
                        IPort port = fieldInfo.GetValue(node) as IPort;
                        var portIndex = port.GetPort().Index;

                        for (int i = 0; i < port.GetDataCount(); i++)
                        {
                            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                            {
                                EditorGUILayout.PrefixLabel($"{(portIndex + i):000} {fieldInfo.Name}");
                                var isInput = port is IInputDataPort || port is IInputTriggerPort;
                                string io = isInput ? "I" : "O";
                                var isData = port is IInputDataPort || port is IOutputDataPort;
                                string dt = isData ? "D" : "T";
                                string m = (port is IMultiPort multiPort)
                                    ? "M" + multiPort.GetDataCount()
                                    : String.Empty;

                                string portInfoDataIndex;
                                if (portIndex + i >= def.PortInfoTable.Length)
                                {
                                    portInfoDataIndex = " PORT MISSING IN PORTINFOTABLE";
                                }
                                else
                                {
                                    var portInfo = def.PortInfoTable[(int)portIndex + i];

                                    portInfoDataIndex = isData && def.HasConnectedValue(port)
                                        ? portInfo.DataOrTriggerIndex.ToString()
                                        : "";
                                }

                                EditorGUILayout.LabelField($"{io}\t{dt}\t{m}\t{portInfoDataIndex}");
                            }
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
