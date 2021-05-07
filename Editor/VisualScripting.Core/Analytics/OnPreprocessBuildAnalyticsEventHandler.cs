using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Unity.VisualScripting.Analytics
{
    internal class OnPreprocessBuildAnalyticsEventHandler : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!EditorAnalytics.enabled || !BoltCore.Configuration.isVisualScriptingUsed)
                return;

            OnPreprocessBuildAnalytics.Send(new OnPreprocessBuildAnalytics.Data()
            {
                Guid = report.summary.guid,
                BuildTarget = report.summary.platform,
                BuildTargetGroup = report.summary.platformGroup
            });
        }
    }
}
