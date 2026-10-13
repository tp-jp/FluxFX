using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;

namespace TpLab.Flux.FX.Editor.Build
{
    public static class FluxParticleGpuKernelResolver
    {
        public static Dictionary<FluxParticleExecutionStage, FluxKernel> Resolve(FluxParticleSimulation simulation)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));

            var result = new Dictionary<FluxParticleExecutionStage, FluxKernel>();
            var kernels = simulation.GetComponentsInChildren<FluxKernel>(true);

            foreach (var kernel in kernels)
            {
                var shader = kernel.Shader;
                if (shader == null) continue;

                var shaderName = shader.name;

                foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
                {
                    if (shaderName != descriptor.SourceShaderName &&
                        !shaderName.StartsWith(descriptor.GeneratedShaderPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (result.ContainsKey(descriptor.Stage))
                    {
                        throw new InvalidOperationException($"Multiple {descriptor.Stage} FluxKernels were found under '{simulation.name}'.");
                    }

                    result.Add(descriptor.Stage, kernel);
                }
            }

            foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
            {
                if (result.ContainsKey(descriptor.Stage)) continue;

                throw new InvalidOperationException($"{descriptor.Stage} FluxKernel was not found under '{simulation.name}'.");
            }

            return result;
        }
    }
}