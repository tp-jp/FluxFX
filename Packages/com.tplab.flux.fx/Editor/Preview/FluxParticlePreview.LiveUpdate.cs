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
            if (!IsPlaying || _velocityMaterial == null || _positionMaterial == null) return;

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

                var compilation = result.Compilation;
                var pipeline = new FluxParticleShaderPipeline();

                var velocityArtifact = pipeline.Build(compilation, FluxParticleExecutionStage.VelocityUpdate);
                var positionArtifact = pipeline.Build(compilation, FluxParticleExecutionStage.PositionUpdate);

                var velocityShader = _velocityMaterial.shader;
                var positionShader = _positionMaterial.shader;

                if (velocityShader == null || velocityShader.name != velocityArtifact.ShaderName)
                {
                    velocityShader = pipeline.GetOrCreate(velocityArtifact);
                }

                if (positionShader == null || positionShader.name != positionArtifact.ShaderName)
                {
                    positionShader = pipeline.GetOrCreate(positionArtifact);
                }

                _velocityMaterial.shader = velocityShader;
                _positionMaterial.shader = positionShader;

                var binder = new FluxParticleGpuMaterialBinder();

                binder.Apply(
                    compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate),
                    _velocityMaterial);

                binder.Apply(
                    compilation.GetStage(FluxParticleExecutionStage.PositionUpdate),
                    _positionMaterial);

                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                Logger.LogError($"Preview update failed: {exception}", _particleSystem);
            }
        }
    }
}