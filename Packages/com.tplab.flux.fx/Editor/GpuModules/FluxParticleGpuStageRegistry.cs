using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// GPU Moduleの実行に対応するStageを管理します。
    /// </summary>
    public static class FluxParticleGpuStageRegistry
    {
        const string ShaderDirectory = "Packages/com.tplab.flux.fx/Runtime/Shaders/";

        static readonly FluxParticleGpuStageDescriptor[] Descriptors =
        {
            new FluxParticleGpuStageDescriptor(
                FluxParticleExecutionStage.VelocityUpdate,
                FluxParticleKernelTarget.Velocity,
                "velocity",
                ShaderDirectory + "Templates/ParticleVelocityUpdate.shader.template",
                "FluxFX/ParticleVelocityUpdate",
                "Hidden/FluxFX/Generated/Velocity_"),

            new FluxParticleGpuStageDescriptor(
                FluxParticleExecutionStage.PositionUpdate,
                FluxParticleKernelTarget.Position,
                "position",
                ShaderDirectory + "Templates/ParticlePositionUpdate.shader.template",
                "FluxFX/ParticlePositionUpdate",
                "Hidden/FluxFX/Generated/PositionUpdate_")
        };

        static readonly IReadOnlyList<FluxParticleGpuStageDescriptor> All = Array.AsReadOnly(Descriptors);

        static readonly Dictionary<FluxParticleExecutionStage, FluxParticleGpuStageDescriptor> ByStage =
            new Dictionary<FluxParticleExecutionStage, FluxParticleGpuStageDescriptor>();

        static FluxParticleGpuStageRegistry()
        {
            var runtimeKeys = new HashSet<string>(StringComparer.Ordinal);
            var shaderPrefixes = new HashSet<string>(StringComparer.Ordinal);
            var kernelTargets = new HashSet<FluxParticleKernelTarget>();

            foreach (var descriptor in Descriptors)
            {
                if (!ByStage.TryAdd(descriptor.Stage, descriptor))
                {
                    throw new InvalidOperationException($"Duplicate GPU Stage: {descriptor.Stage}");
                }

                if (!runtimeKeys.Add(descriptor.RuntimeKey))
                {
                    throw new InvalidOperationException($"Duplicate GPU Runtime key: {descriptor.RuntimeKey}");
                }

                if (!shaderPrefixes.Add(descriptor.GeneratedShaderPrefix))
                {
                    throw new InvalidOperationException($"Duplicate generated Shader prefix: {descriptor.GeneratedShaderPrefix}");
                }

                if (!kernelTargets.Add(descriptor.KernelTarget))
                {
                    throw new InvalidOperationException($"Duplicate GPU Kernel target: {descriptor.KernelTarget}");
                }
            }
        }

        public static IReadOnlyList<FluxParticleGpuStageDescriptor> GetAll()
        {
            return All;
        }

        public static bool TryGet(FluxParticleExecutionStage stage, out FluxParticleGpuStageDescriptor descriptor)
        {
            return ByStage.TryGetValue(stage, out descriptor);
        }

        public static FluxParticleGpuStageDescriptor GetRequired(FluxParticleExecutionStage stage)
        {
            if (TryGet(stage, out var descriptor)) return descriptor;

            throw new InvalidOperationException($"GPU Modules are not supported for stage: {stage}");
        }
    }
}