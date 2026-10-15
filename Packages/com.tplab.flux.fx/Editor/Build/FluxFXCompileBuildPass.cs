using TpLab.Flux.Editor;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// Particle Systemをコンパイルし、ShaderとBuild結果を後続Passへ渡します。
    /// </summary>
    public class FluxFXCompileBuildPass : PassBase
    {
        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.Before<FluxBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);
            var assignedCount = 0;

            foreach (var authoring in authorings)
            {
                var particleSystem = authoring.GetComponent<FluxParticleSystem>();
                if (particleSystem == null) continue;
                FluxParticleProgramBuild.Apply(authoring, particleSystem);
                assignedCount++;
            }

            Logger.Log($"Particle compilation completed. Systems: {assignedCount}");
        }
    }
}