using System;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// Particle SystemのGPUコンパイル結果、Shader Artifact、Runtime JSONを保持します。
    /// </summary>
    public sealed class FluxParticleBuildResult
    {
        /// <summary>
        /// 検証済みのGPU Compilation Modelを取得します。
        /// </summary>
        public FluxParticleGpuCompilation Compilation { get; }

        /// <summary>
        /// Velocity Update用のShader Artifactを取得します。
        /// </summary>
        public ShaderArtifact VelocityArtifact { get; }

        /// <summary>
        /// Runtime用JSONを取得します。
        /// </summary>
        public string RuntimeJson { get; }

        /// <summary>
        /// Particle SystemのBuild結果を作成します。
        /// </summary>
        /// <param name="compilation">検証済みのGPU Compilation Model。</param>
        /// <param name="velocityArtifact">Velocity Update用のShader Artifact。</param>
        /// <param name="runtimeJson">Runtime用JSON。</param>
        /// <exception cref="ArgumentNullException">引数がnullの場合。</exception>
        /// <exception cref="ArgumentException">Runtime JSONが空の場合。</exception>
        public FluxParticleBuildResult(
            FluxParticleGpuCompilation compilation,
            ShaderArtifact velocityArtifact,
            string runtimeJson)
        {
            Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            VelocityArtifact = velocityArtifact ?? throw new ArgumentNullException(nameof(velocityArtifact));

            if (runtimeJson == null) throw new ArgumentNullException(nameof(runtimeJson));
            if (string.IsNullOrWhiteSpace(runtimeJson)) throw new ArgumentException("Runtime JSON must not be empty.", nameof(runtimeJson));

            RuntimeJson = runtimeJson;
        }
    }
}