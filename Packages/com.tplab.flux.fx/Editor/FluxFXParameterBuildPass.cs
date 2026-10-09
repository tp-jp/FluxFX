using System;
using TpLab.Flux.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor
{
    public class FluxFXParameterBuildPass : PassBase
    {
        const string VelocityShaderPrefix = "Hidden/FluxFX/Generated/Velocity_";

        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.After<FluxBuildPass>();
            builder.Before<FluxFXBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            var registry = new FluxParticleGpuModuleRegistry();
            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);
            var appliedCount = 0;

            foreach (var authoring in authorings)
            {
                if (authoring.GetComponent<FluxParticleSystem>() == null) continue;

                var plan = FluxParticleCompilePlan.Create(authoring);
                var simulation = authoring.GetComponentInChildren<FluxParticleSimulation>(true);

                if (simulation == null)
                    throw new InvalidOperationException($"FluxParticleSimulation was not found under '{authoring.name}'.");

                var velocityKernel = FindVelocityKernel(simulation);
                if (velocityKernel == null)
                    throw new InvalidOperationException($"Velocity FluxKernel was not found under '{authoring.name}'.");

                var material = velocityKernel.GetProgramVariable("material") as Material;
                if (material == null)
                    throw new InvalidOperationException($"Velocity Kernel Material was not found under '{authoring.name}'.");

                var collector = new FluxParticleParameterCollector();

                foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
                {
                    if (!registry.TryGet(module.GetType(), out var definition)) continue;

                    definition.CollectParameters(module, collector);
                }

                collector.Apply(material);
                appliedCount++;

                Logger.Log($"GPU parameters applied: {authoring.name}", authoring);
            }

            Logger.Log($"Parameter build completed. Systems: {appliedCount}");
        }

        static FluxKernel FindVelocityKernel(FluxParticleSimulation simulation)
        {
            var kernels = simulation.GetComponentsInChildren<FluxKernel>(true);
            FluxKernel result = null;

            foreach (var kernel in kernels)
            {
                var shader = kernel.Shader;
                if (shader == null || !shader.name.StartsWith(VelocityShaderPrefix, StringComparison.Ordinal)) continue;

                if (result != null)
                    throw new InvalidOperationException($"Multiple Velocity Kernels were found under '{simulation.name}'.");

                result = kernel;
            }

            return result;
        }
    }
}
