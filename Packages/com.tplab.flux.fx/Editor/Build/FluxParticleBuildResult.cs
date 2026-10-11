using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// Particle SystemのGPUコンパイル結果、Stage別Shader Artifact、Runtime JSONを保持します。
    /// </summary>
    public sealed class FluxParticleBuildResult
    {
        readonly Dictionary<FluxParticleExecutionStage, ShaderArtifact> _artifacts;

        public FluxParticleGpuCompilation Compilation { get; }

        public IReadOnlyDictionary<FluxParticleExecutionStage, ShaderArtifact> ShaderArtifacts { get; }

        public ShaderArtifact VelocityArtifact => GetArtifact(FluxParticleExecutionStage.VelocityUpdate);

        public string RuntimeJson { get; }

        public FluxParticleBuildResult(
            FluxParticleGpuCompilation compilation,
            IEnumerable<ShaderArtifact> artifacts,
            string runtimeJson)
        {
            Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            if (artifacts == null) throw new ArgumentNullException(nameof(artifacts));
            if (runtimeJson == null) throw new ArgumentNullException(nameof(runtimeJson));
            if (string.IsNullOrWhiteSpace(runtimeJson)) throw new ArgumentException("Runtime JSON must not be empty.", nameof(runtimeJson));

            _artifacts = new Dictionary<FluxParticleExecutionStage, ShaderArtifact>();

            foreach (var artifact in artifacts)
            {
                if (artifact == null) throw new ArgumentException("Shader Artifact cannot be null.", nameof(artifacts));

                if (!FluxParticleGpuStageRegistry.TryGet(artifact.Stage, out _))
                {
                    throw new ArgumentException($"Unsupported Shader Artifact stage: {artifact.Stage}", nameof(artifacts));
                }

                if (_artifacts.ContainsKey(artifact.Stage))
                {
                    throw new ArgumentException($"Duplicate Shader Artifact stage: {artifact.Stage}", nameof(artifacts));
                }

                _artifacts.Add(artifact.Stage, artifact);
            }

            foreach (var descriptor in FluxParticleGpuStageRegistry.GetAll())
            {
                if (Compilation.GetStage(descriptor.Stage) == null)
                {
                    throw new ArgumentException($"Compilation stage was not found: {descriptor.Stage}", nameof(compilation));
                }

                if (!_artifacts.ContainsKey(descriptor.Stage))
                {
                    throw new ArgumentException($"Shader Artifact was not found: {descriptor.Stage}", nameof(artifacts));
                }
            }

            ShaderArtifacts = new System.Collections.ObjectModel.ReadOnlyDictionary<FluxParticleExecutionStage, ShaderArtifact>(_artifacts);
            RuntimeJson = runtimeJson;
        }

        public FluxParticleBuildResult(
            FluxParticleGpuCompilation compilation,
            ShaderArtifact velocityArtifact,
            string runtimeJson)
            : this(compilation, new[]
            {
                velocityArtifact,
                new FluxParticleShaderPipeline().Build(compilation, FluxParticleExecutionStage.PositionUpdate)
            }, runtimeJson)
        {
        }

        public ShaderArtifact GetArtifact(FluxParticleExecutionStage stage)
        {
            if (_artifacts.TryGetValue(stage, out var artifact)) return artifact;

            throw new InvalidOperationException($"Shader Artifact was not found: {stage}");
        }
    }
}