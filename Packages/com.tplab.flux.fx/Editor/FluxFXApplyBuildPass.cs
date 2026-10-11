using System;
using TpLab.Flux.Editor;
using TpLab.Flux.FX.Editor.Build;
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
    /// <summary>
    /// コンパイル済みのParticle System設定をRuntimeコンポーネントへ適用します。
    /// </summary>
    public class FluxFXApplyBuildPass : PassBase
    {
        const string VelocityShaderPrefix = "Hidden/FluxFX/Generated/Velocity_";

        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.After<FluxBuildPass>();
            builder.After<FluxFXCompileBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            var session = context.Data.GetRequired<FluxFXCompilationSession>();
            var binder = new FluxParticleGpuMaterialBinder();
            var lutBaker = new FluxParticleLutBaker();

            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);
            var appliedCount = 0;

            foreach (var authoring in authorings)
            {
                var particleSystem = authoring.GetComponent<FluxParticleSystem>();
                if (particleSystem == null) continue;

                var result = session.GetRequired(authoring);
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

                var material = velocityKernel.GetProgramVariable("material") as Material;

                if (material == null)
                {
                    throw new InvalidOperationException($"Velocity Kernel Material was not found under '{authoring.name}'.");
                }

                var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

                if (stage == null)
                {
                    throw new InvalidOperationException($"Velocity Compilation Stage was not found for '{authoring.name}'.");
                }

                binder.Apply(stage, material);
                particleSystem.SetProgramVariable("compiledParameters", result.RuntimeJson);

                var particleState = authoring.GetComponentInChildren<FluxParticleState>(true);

                if (particleState == null)
                {
                    Logger.LogError($"FluxParticleState was not found on '{authoring.name}'.", authoring);
                    continue;
                }

                var buffers = particleState.GetComponentsInChildren<FluxBuffer>(true);

                foreach (var buffer in buffers)
                {
                    buffer.SetProgramVariable("initialCount", particleSystem.ParticleCount);
                }

                var particleRenderer = authoring.GetComponentInChildren<FluxParticleRenderer>(true);

                if (particleRenderer == null)
                {
                    Logger.LogError($"FluxParticleRenderer was not found under '{authoring.name}'.", authoring);
                    continue;
                }

                var renderMode = authoring.Render.Mode;
                var sourceMesh = authoring.Render.Mesh;
                var particleMaterial = authoring.Render.Material;
                var blendMode = authoring.Render.BlendMode;
                var colorOverLifetimeLut = lutBaker.BakeColorOverLifetime(authoring);
                var sizeOverLifetimeLut = lutBaker.BakeSizeOverLifetime(authoring);
                var rotationOverLifetimeLut = lutBaker.BakeRotationOverLifetime(authoring);

                particleRenderer.SetProgramVariable("renderMode", renderMode);
                particleRenderer.SetProgramVariable("sourceMesh", sourceMesh);
                particleRenderer.SetProgramVariable("particleMaterial", particleMaterial);
                particleRenderer.SetProgramVariable("blendMode", blendMode);
                particleRenderer.SetProgramVariable("colorOverLifetimeLut", colorOverLifetimeLut);
                particleRenderer.SetProgramVariable("sizeOverLifetimeLut", sizeOverLifetimeLut);
                particleRenderer.SetProgramVariable("rotationOverLifetimeLut", rotationOverLifetimeLut);

                appliedCount++;

                Logger.Log($"Particle settings applied: {authoring.name}", authoring);
            }

            UnityEditor.AssetDatabase.SaveAssets();

            Logger.Log($"Particle apply completed. Systems: {appliedCount}");
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
                {
                    throw new InvalidOperationException($"Multiple Velocity FluxKernels were found under '{simulation.name}'.");
                }

                result = kernel;
            }

            return result;
        }
    }
}