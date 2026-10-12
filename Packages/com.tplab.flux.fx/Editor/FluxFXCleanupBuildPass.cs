using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using UnityEditor.SceneManagement;

namespace TpLab.Flux.FX.Editor
{
    /// <summary>
    /// 使用されていない生成ShaderとIncludeをCleanupします。
    /// </summary>
    public class FluxFXCleanupBuildPass : PassBase
    {
        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.After<FluxFXApplyBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                if (!EditorSceneManager.GetSceneAt(i).isDirty) continue;

                Logger.Log("Shader Cleanup skipped because the scene has unsaved changes.");
                return;
            }

            var session = context.Data.GetRequired<FluxFXCompilationSession>();

            new ShaderArtifactAutoCleanupExecutor().Execute(session);
        }
    }
}