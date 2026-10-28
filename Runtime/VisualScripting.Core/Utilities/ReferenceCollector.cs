using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.VisualScripting
{
    public static class ReferenceCollector
    {
        public static event Action onSceneUnloaded;

        static void HandleSceneUnloaded(Scene scene)
        {
            onSceneUnloaded?.Invoke();
        }

        // RuntimeVSUsageUtility calls Initialize at every BeforeSceneLoad and nothing clears SceneManager.sceneUnloaded between play sessions
        // without a domain reload, so subscribe a named handler idempotently rather than adding a fresh lambda each time.
        internal static void Initialize()
        {
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetStaticsOnLoad()
        {
            onSceneUnloaded = null;
        }
#endif
    }
}
