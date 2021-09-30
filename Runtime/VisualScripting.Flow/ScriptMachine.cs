using Unity.VisualScripting.Interpreter;
using UnityEngine;

namespace Unity.VisualScripting
{
    [AddComponentMenu("Visual Scripting/Script Machine")]
    [RequireComponent(typeof(Variables))]
    [DisableAnnotation]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.visualscripting@latest/index.html?subfolder=/manual/vs-graphs-machines-macros.html")]
    [RenamedFrom("Bolt.FlowMachine")]
    [RenamedFrom("Unity.VisualScripting.FlowMachine")]
    public sealed class ScriptMachine : EventMachine<FlowGraph, ScriptGraphAsset>
    {
        public bool UseNewInterpreter;
        private GraphInstance m_GraphInstance;
        private Coroutine m_RunningCoroutine;

        protected override bool IsUsingNewRuntime => UseNewInterpreter && graph?.RuntimeGraphAsset;

        public override FlowGraph DefaultGraph()
        {
            return FlowGraph.WithStartUpdate();
        }

        protected override void OnEnable()
        {
            if (IsUsingNewRuntime)
            {
                SetupNewRuntime();
            }
            else if (hasGraph)
            {
                graph.StartListening(reference);
            }

            base.OnEnable();
        }

        private void SetupNewRuntime()
        {
            if (m_GraphInstance == null)
                m_GraphInstance = new GraphInstance(gameObject, graph.RuntimeGraphAsset.GraphDefinition,
                    graph.RuntimeGraphAsset.Hash);
            else
            {
                StopCoroutine(m_RunningCoroutine);
            }
            m_RunningCoroutine = StartCoroutine(m_GraphInstance.InterpreterCoroutine());
        }

        protected override void OnInstantiateWhileEnabled()
        {
            if (IsUsingNewRuntime)
            {
                SetupNewRuntime();
            }
            else if (hasGraph)
            {
                graph.StartListening(reference);
            }

            base.OnInstantiateWhileEnabled();
        }

        protected override void Update()
        {
#if UNITY_EDITOR
            if (IsUsingNewRuntime && m_GraphInstance != null && m_GraphInstance.Hash != graph.RuntimeGraphAsset.Hash)
            {
                Debug.Log($"Live reload {m_GraphInstance.Hash} -> {graph.RuntimeGraphAsset.Hash}");
                StopCoroutine(m_RunningCoroutine);
                m_GraphInstance.Dispose();
                m_GraphInstance = null;
                SetupNewRuntime();
            }
#endif
            base.Update();
        }

        protected override void OnUninstantiateWhileEnabled()
        {
            base.OnUninstantiateWhileEnabled();

            if (hasGraph)
            {
                graph.StopListening(reference);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (IsUsingNewRuntime)
            {
            }
            else if (hasGraph)
            {
                graph.StopListening(reference);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_GraphInstance?.Dispose();
        }

        [ContextMenu("Show Data...")]
        protected override void ShowData()
        {
            base.ShowData();
        }
    }
}
