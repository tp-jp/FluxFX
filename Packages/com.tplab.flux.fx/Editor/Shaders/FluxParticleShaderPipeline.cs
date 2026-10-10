using System;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderPipeline
    {
        readonly FluxParticleShaderCompiler _compiler;
        readonly ShaderArtifactBuilder _builder;
        readonly ShaderArtifactStore _store;

        public FluxParticleShaderPipeline(FluxParticleGpuModuleRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            _compiler = new FluxParticleShaderCompiler(registry);
            _builder = new ShaderArtifactBuilder();
            _store = new ShaderArtifactStore();
        }

        public ShaderArtifact BuildVelocity(FluxParticleCompilePlan plan)
        {
            var draft = _compiler.CompileVelocity(plan);
            return _builder.Build(draft);
        }

        public Shader GetOrCreate(ShaderArtifact artifact)
        {
            return _store.GetOrCreate(artifact);
        }
    }
}