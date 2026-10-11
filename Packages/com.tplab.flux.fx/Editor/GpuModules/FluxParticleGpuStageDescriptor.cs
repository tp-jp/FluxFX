using System;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    /// <summary>
    /// GPU Moduleを実行するStageと、Shader・Kernel・Runtimeの対応情報を保持します。
    /// </summary>
    public sealed class FluxParticleGpuStageDescriptor
    {
        public FluxParticleExecutionStage Stage { get; }

        public FluxParticleKernelTarget KernelTarget { get; }

        public string RuntimeKey { get; }

        public string ShaderTemplatePath { get; }

        public string SourceShaderName { get; }

        public string GeneratedShaderPrefix { get; }

        public FluxParticleGpuStageDescriptor(
            FluxParticleExecutionStage stage,
            FluxParticleKernelTarget kernelTarget,
            string runtimeKey,
            string shaderTemplatePath,
            string sourceShaderName,
            string generatedShaderPrefix)
        {
            if (stage == FluxParticleExecutionStage.None || ((int)stage & ((int)stage - 1)) != 0)
            {
                throw new ArgumentException($"GPU Stage must contain exactly one execution stage: {stage}.", nameof(stage));
            }

            if (string.IsNullOrWhiteSpace(runtimeKey)) throw new ArgumentException("Runtime key is required.", nameof(runtimeKey));
            if (string.IsNullOrWhiteSpace(shaderTemplatePath)) throw new ArgumentException("Shader template path is required.", nameof(shaderTemplatePath));
            if (string.IsNullOrWhiteSpace(sourceShaderName)) throw new ArgumentException("Source Shader name is required.", nameof(sourceShaderName));
            if (string.IsNullOrWhiteSpace(generatedShaderPrefix)) throw new ArgumentException("Generated Shader prefix is required.", nameof(generatedShaderPrefix));

            Stage = stage;
            KernelTarget = kernelTarget;
            RuntimeKey = runtimeKey;
            ShaderTemplatePath = shaderTemplatePath;
            SourceShaderName = sourceShaderName;
            GeneratedShaderPrefix = generatedShaderPrefix;
        }
    }
}