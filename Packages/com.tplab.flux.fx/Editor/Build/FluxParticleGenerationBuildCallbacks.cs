using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// Unity Buildの開始と終了を生成セッションへ通知します。
    /// </summary>
    public sealed class FluxParticleGenerationBuildCallbacks : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            FluxParticleGenerationLifecycle.BeginPlayerBuild();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            FluxParticleGenerationLifecycle.EndPlayerBuild(report.summary.result == BuildResult.Succeeded);
        }
    }
}
