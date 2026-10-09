
using System;
using TpLab.Flux.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor
{
    public class FluxFXShaderBuildPass : PassBase
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
            var compiler = new FluxParticleShaderCompiler(registry);
            var cache = new FluxParticleShaderCache();

            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);
            var assignedCount = 0;

            foreach (var authoring in authorings)
            {
                var particleSystem = authoring.GetComponent<FluxParticleSystem>();
                if (particleSystem == null) continue;

                var simulation = authoring.GetComponentInChildren<FluxParticleSimulation>(true);
                if (simulation == null)
                    throw new InvalidOperationException($"FluxParticleSimulation was not found under '{authoring.name}'.");

                var velocityKernel = FindVelocityKernel(simulation);
                if (velocityKernel == null)
                    throw new InvalidOperationException($"Velocity FluxKernel was not found under '{authoring.name}'.");

                var plan = FluxParticleCompilePlan.Create(authoring);
                var compilation = compiler.CompileVelocity(plan);
                var shader = cache.GetOrCreate(compilation);

                velocityKernel.SetProgramVariable("shader", shader);
                assignedCount++;

                Logger.Log($"[FluxFX Shader] Assigned {shader.name} to {authoring.name}", authoring);
            }

            Logger.Log($"[FluxFX Shader] Build Pass completed. Assigned systems: {assignedCount}");
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
                    continue;

                if (result != null)
                    throw new InvalidOperationException($"Multiple Velocity FluxKernels were found under '{simulation.name}'.");

                result = kernel;
            }

            return result;
        }
    }
}
