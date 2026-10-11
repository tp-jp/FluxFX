using System;
using System.Collections.Generic;
using TpLab.Flux.Editor;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor
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
            var registry = new FluxParticleGpuModuleRegistry();
            var gpuCompiler = new FluxParticleGpuCompiler(registry);
            var runtimeCompiler = new FluxParticleCompiler();
            var pipeline = new FluxParticleShaderPipeline();
            var session = new FluxFXCompilationSession();

            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);
            var assignedCount = 0;

            foreach (var authoring in authorings)
            {
                var particleSystem = authoring.GetComponent<FluxParticleSystem>();
                if (particleSystem == null) continue;

                var simulation = authoring.GetComponentInChildren<FluxParticleSimulation>(true);

                if (simulation == null)
                {
                    throw new InvalidOperationException($"FluxParticleSimulation was not found under '{authoring.name}'.");
                }

                var kernels = FluxParticleGpuKernelResolver.Resolve(simulation);
                var plan = FluxParticleCompilePlan.Create(authoring);
                var result = gpuCompiler.Compile(plan);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Particle compilation failed for '{authoring.name}':\n{result.Validation.GetReport()}");
                }

                var artifacts = new List<ShaderArtifact>();

                foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
                {
                    var artifact = pipeline.Build(result.Compilation, descriptor.Stage);
                    var shader = pipeline.GetOrCreate(artifact);
                    var kernel = kernels[descriptor.Stage];

                    kernel.SetProgramVariable("shader", shader);
                    artifacts.Add(artifact);

                    Logger.Log($"[FluxFX] Shader assigned: {shader.name} to {authoring.name}", authoring);
                }

                var runtimeJson = runtimeCompiler.Compile(authoring, plan, result.Compilation);

                session.Add(authoring, new FluxParticleBuildResult(result.Compilation, artifacts, runtimeJson));
                assignedCount++;
            }

            context.Data.Add(session);

            Logger.Log($"[FluxFX] Particle compilation completed. Systems: {assignedCount}");
        }
    }
}