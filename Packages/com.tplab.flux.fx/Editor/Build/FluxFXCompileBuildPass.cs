using TpLab.Flux.Editor;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using System;
using System.Linq;

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
            var authorings = context.Scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<FluxParticleAuthoring>(true)).ToArray();
            if (authorings.Length == 0) return;
            var assignedCount = 0;

            var session = FluxParticleGenerationLifecycle.Current;
            var ownsSession = session == null;
            if (ownsSession)
            {
                session = FluxParticleGenerationSession.Begin("Manual");
            }
            try
            {
                foreach (var authoring in authorings)
                {
                    var particleSystem = authoring.GetComponent<FluxParticleSystem>();
                    if (particleSystem == null) continue;
                    FluxParticleProgramBuild.Apply(authoring, particleSystem, session);
                    assignedCount++;
                }
            }
            catch (Exception error)
            {
                session.RecordFailure(error.Message);
                throw;
            }
            finally
            {
                if (ownsSession)
                {
                    session.Dispose();
                }
            }

            Logger.Log($"Particle compilation completed. Systems: {assignedCount}");
        }
    }
}