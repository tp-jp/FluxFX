using System;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderPipeline
    {
        readonly FluxParticleShaderGenerator _generator;
        readonly ShaderArtifactBuilder _builder;
        readonly ShaderArtifactStore _store;

        public FluxParticleShaderPipeline()
        {
            _generator = new FluxParticleShaderGenerator();
            _builder = new ShaderArtifactBuilder();
            _store = new ShaderArtifactStore();
        }

        public ShaderArtifact Build(
            FluxParticleGpuCompilation compilation,
            FluxParticleExecutionStage stage)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            var draft = _generator.Generate(compilation, stage);

            return _builder.Build(draft);
        }

        public ShaderArtifact BuildVelocity(FluxParticleGpuCompilation compilation)
        {
            return Build(compilation, FluxParticleExecutionStage.VelocityUpdate);
        }

        public Shader GetOrCreate(ShaderArtifact artifact)
        {
            return _store.GetOrCreate(artifact);
        }
    }
}