using System;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders.Artifacts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderPipeline
    {
        readonly FluxParticleGpuCompiler _compiler;
        readonly FluxParticleShaderGenerator _generator;
        readonly ShaderArtifactBuilder _builder;
        readonly ShaderArtifactStore _store;

        public FluxParticleShaderPipeline(FluxParticleGpuModuleRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            _compiler = new FluxParticleGpuCompiler(registry);
            _generator = new FluxParticleShaderGenerator();
            _builder = new ShaderArtifactBuilder();
            _store = new ShaderArtifactStore();
        }

        public ShaderArtifact BuildVelocity(FluxParticleGpuCompilation compilation)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            var draft = _generator.GenerateVelocity(compilation);
            return _builder.Build(draft);
        }

        public ShaderArtifact BuildVelocity(FluxParticleCompilePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var result = _compiler.Compile(plan);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"GPU Module compilation failed:\n{result.Validation.GetReport()}");
            }

            return BuildVelocity(result.Compilation);
        }

        public Shader GetOrCreate(ShaderArtifact artifact)
        {
            return _store.GetOrCreate(artifact);
        }
    }
}