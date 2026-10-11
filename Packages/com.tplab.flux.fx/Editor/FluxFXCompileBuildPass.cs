using System;
using TpLab.Flux.Editor;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
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
        const string SourceShaderName = "FluxFX/ParticleVelocityUpdate";
        const string GeneratedShaderPrefix = "Hidden/FluxFX/Generated/Velocity_";

        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.Before<FluxBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            var registry = new FluxParticleGpuModuleRegistry();
            var gpuCompiler = new FluxParticleGpuCompiler(registry);
            var runtimeCompiler = new FluxParticleCompiler();
            var pipeline = new FluxParticleShaderPipeline(registry);
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

                var velocityKernel = FindVelocityKernel(simulation);

                if (velocityKernel == null)
                {
                    throw new InvalidOperationException($"Velocity FluxKernel was not found under '{authoring.name}'.");
                }

                var plan = FluxParticleCompilePlan.Create(authoring);
                var result = gpuCompiler.Compile(plan);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Particle compilation failed for '{authoring.name}':\n{result.Validation.GetReport()}");
                }

                var artifact = pipeline.BuildVelocity(result.Compilation);
                var runtimeJson = runtimeCompiler.Compile(authoring, plan, result.Compilation);
                var shader = pipeline.GetOrCreate(artifact);

                velocityKernel.SetProgramVariable("shader", shader);

                session.Add(authoring, new FluxParticleBuildResult(result.Compilation, artifact, runtimeJson));

                assignedCount++;

                Logger.Log($"Shader assigned: {shader.name} to {authoring.name}", authoring);
            }

            context.Data.Add(session);

            Logger.Log($"Particle compilation completed. Systems: {assignedCount}");
        }

        static FluxKernel FindVelocityKernel(FluxParticleSimulation simulation)
        {
            var kernels = simulation.GetComponentsInChildren<FluxKernel>(true);
            FluxKernel result = null;

            foreach (var kernel in kernels)
            {
                var shader = kernel.Shader;
                if (shader == null) continue;

                var shaderName = shader.name;

                if (shaderName != SourceShaderName && !shaderName.StartsWith(GeneratedShaderPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (result != null)
                {
                    throw new InvalidOperationException($"Multiple Velocity FluxKernels were found under '{simulation.name}'.");
                }

                result = kernel;
            }

            return result;
        }
    }
}