using System;
using System.Collections.Generic;
using Unity.VisualScripting.Interpreter;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    [GraphContext(typeof(FlowGraph))]
    public class FlowGraphContext : GraphContext<FlowGraph, FlowCanvas>
    {
        public FlowGraphContext(GraphReference reference) : base(reference)
        {
        }

        public override string windowTitle => "Script Graph";

        protected override IEnumerable<ISidebarPanelContent> SidebarPanels()
        {
            yield return new GraphInspectorPanel(this);
            yield return new VariablesPanel(this);
        }

        internal FlowGraphContext NestedContext(SubgraphUnit nester)
        {
            return new FlowGraphContext(reference.ChildReference(nester, false));
        }

        protected internal override void Translate()
        {
            var scriptMachine = reference?.machine as ScriptMachine;
            if (scriptMachine == null || !scriptMachine.UseNewInterpreter)
            {
                var analyzer = graph.Analyser<FlowGraphTranslator.NewRuntimeAnalyzer>(this);
                analyzer.Messages = new List<Warning>();
                analyzer.isDirty = true;
                analyzer.Validate();
                return;
            }

            var referenceScriptableObject = reference.scriptableObject;
            if (!referenceScriptableObject)
            {
                referenceScriptableObject = reference?.machine?.nest?.macro as ScriptableObject;
            }
            if (!graph.RuntimeGraphAsset)
            {
                if (!graph.RuntimeGraphAsset)
                    graph.RuntimeGraphAsset = ScriptableObject.CreateInstance<RuntimeGraphAsset>();
                if (referenceScriptableObject)
                {
                    var assetPath = AssetDatabase.GetAssetPath(referenceScriptableObject);
                    // temp assets, usually for tests, are not saved and don't have a path
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        // TODO Rohit: when translating a subasset, assetpath must be the path of the subgraph, not the parent graph using it
                        AssetDatabase.AddObjectToAsset(graph.RuntimeGraphAsset, assetPath);
                        AssetDatabase.SetMainObject(referenceScriptableObject, assetPath);
                    }
                }
            }
            else
            {
                if (referenceScriptableObject)
                {
                    var assetPath = AssetDatabase.GetAssetPath(referenceScriptableObject);
                    var runtimeAssetPath = AssetDatabase.GetAssetPath(graph.RuntimeGraphAsset);
                    if (!string.IsNullOrEmpty(assetPath) && string.IsNullOrEmpty(runtimeAssetPath))
                    {
                        AssetDatabase.AddObjectToAsset(graph.RuntimeGraphAsset, referenceScriptableObject);
                        AssetDatabase.SetMainObject(referenceScriptableObject, assetPath);
                    }
                }
            }

            // Debug.Log("Translate " + this);
            TranslationOptions options = TranslationOptions.None;
            if (EditorApplication.isPlaying)
                options &= ~TranslationOptions.CodegenApiNodes;

            var newRuntimeAnalyzer = graph.Analyser<FlowGraphTranslator.NewRuntimeAnalyzer>(this);

            List<Warning> messages = new List<Warning>();

            var graphTransforms = new List<GraphTransform>();
            if (EditorPrefs.GetBool(ConstantFoldingTransform.k_InternalVSToggleConstantfolding))
                graphTransforms.Add(new ConstantFoldingTransform());

            var flowGraphTranslator = new FlowGraphTranslator { Transforms = graphTransforms };
            flowGraphTranslator.Translate(this, graph, graph.RuntimeGraphAsset, messages, options);
            newRuntimeAnalyzer.Messages = messages;
            newRuntimeAnalyzer.isDirty = true;
            newRuntimeAnalyzer.Validate();
        }

        public override void OnRecordFrameTrace(uint hash, int frame, GameObject gameObject, DotsFrameTrace.RecordedStep step)
        {
            if (!BoltFlow.Configuration.showConnectionValues)
                return;
            // TODO: short-term optimization, too slow if we record everything. the recording boxes way too much. try to record Value structs instead
            if (reference.gameObject != gameObject &&
                (!reference.component || reference.component.gameObject != gameObject))
                return;
            // Debug.Log($"Frame trace {hash} {frame} {gameObject}");
            var debugData = graph.RuntimeGraphAsset.DebugData;
            IGraphElement element;
            switch (step.Type)
            {
                case DotsFrameTrace.StepType.ExecutedNode:
                    {
                        var guid = debugData.GetNodeGuid(step.NodeId);
                        if (graph.elements.TryGetValue(guid, out element) && element is IUnit unit)
                        {
                            var editorData = this.reference.GetElementDebugData<IUnitDebugData>(unit);
                            editorData.lastInvokeFrame = EditorTimeBinding.frame;
                            editorData.lastInvokeTime = EditorTimeBinding.time;
                        }

                        break;
                    }
                case DotsFrameTrace.StepType.TriggeredPort:
                    {
                        ProcessConnection<ControlConnection>(true, false);

                        break;
                    }
                case DotsFrameTrace.StepType.WrittenValue:
                    {
                        break;
                    }
                case DotsFrameTrace.StepType.ReadValue:
                    {
                        ProcessConnection<ValueConnection>(false, true);
                        break;
                    }
                case DotsFrameTrace.StepType.Error:
                    {
                        var guid = debugData.GetNodeGuid(step.NodeId);
                        if (graph.elements.TryGetValue(guid, out element) && element is IUnit unit)
                        {
                            var editorData = this.reference.GetElementDebugData<IGraphElementDebugData>(unit);
                            editorData.runtimeException = step.ErrorMessage;
                        }

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            void ProcessConnection<T>(bool matchSource, bool recordValue) where T : IUnitConnection
            {
                if (debugData.GetNodeAndPortFromRuntimePort(step.Port, out var portElement) && portElement is IUnitPort unitPort)
                {
                    foreach (var elt in graph.elements)
                    {
                        if (elt is T connection &&
                            (matchSource ? connection.source == unitPort : connection.destination == unitPort))
                        {
                            var connectionEditorData =
                                reference.GetElementDebugData<IUnitConnectionDebugData>(connection);
                            connectionEditorData.lastInvokeFrame = EditorTimeBinding.frame;
                            connectionEditorData.lastInvokeTime = EditorTimeBinding.time;
                            if (recordValue && connectionEditorData is ValueConnection.DebugData valueData)
                            {
                                valueData.lastValue = step.Value.Box();
                                valueData.assignedLastValue = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
