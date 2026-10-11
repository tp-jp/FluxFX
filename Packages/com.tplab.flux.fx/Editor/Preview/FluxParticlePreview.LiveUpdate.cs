using System;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;

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
                var compiler = new FluxParticleGpuCompiler(registry);
                var result = compiler.Compile(plan);

                if (!result.Success)
                {
                    Logger.LogError($"Preview GPU Module compilation failed:\n{result.Validation.GetReport()}", _particleSystem);
                    return;
                }

                var pipeline = new FluxParticleShaderPipeline();
                var artifact = pipeline.BuildVelocity(result.Compilation);

                if (_velocityMaterial.shader.name != artifact.ShaderName)
                {
                    _velocityMaterial.shader = pipeline.GetOrCreate(artifact);
                }

                var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
                new FluxParticleGpuMaterialBinder().Apply(stage, _velocityMaterial);

                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                Logger.LogError($"Preview update failed: {exception}", _particleSystem);
            }
        }
    }
}