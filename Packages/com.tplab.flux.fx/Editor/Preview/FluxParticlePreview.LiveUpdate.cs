using System;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Preview
{
    public sealed partial class FluxParticlePreview
    {
        public void OnAuthoringChanged()
        {
            if (!IsPlaying || _velocityMaterial == null) return;

            try
            {
                var plan = FluxParticleCompilePlan.Create(_authoring);
                var registry = new FluxParticleGpuModuleRegistry();

                var standardValidation = new FluxParticleCompileValidator().Validate(plan);
                if (standardValidation.HasErrors)
                {
                    Logger.LogError($"Preview validation failed:\n{standardValidation.GetReport()}", _particleSystem);
                    return;
                }

                var gpuValidation = new FluxParticleGpuModuleValidator(registry).Validate(plan);
                if (gpuValidation.HasErrors)
                {
                    Logger.LogError($"Preview GPU Module validation failed:\n{gpuValidation.GetReport()}", _particleSystem);
                    return;
                }

                var collector = new FluxParticleParameterCollector();

                foreach (var module in plan.GetModules(FluxParticleExecutionStage.VelocityUpdate))
                {
                    if (!registry.TryGet(module.GetType(), out var definition)) continue;

                    definition.CollectParameters(module, collector);
                }

                var compiler = new FluxParticleShaderCompiler(registry);
                var compilation = compiler.CompileVelocity(plan);

                if (_velocityMaterial.shader.name != compilation.ShaderName)
                {
                    var cache = new FluxParticleShaderCache();
                    var shader = cache.GetOrCreate(compilation);

                    _velocityMaterial.shader = shader;
                }

                collector.Apply(_velocityMaterial);

                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                Logger.LogError($"Preview update failed: {exception}", _particleSystem);
            }
        }
    }
}