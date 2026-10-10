using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifact
    {
        public FluxParticleExecutionStage Stage { get; }
        public string ArtifactId { get; }
        public string ShaderName { get; }
        public string AssetPath { get; }
        public string Source { get; }
        public ShaderIncludeArtifactSet Includes { get; }

        public ShaderArtifact(FluxParticleExecutionStage stage, string artifactId, string shaderName, string assetPath, string source, ShaderIncludeArtifactSet includes)
        {
            Stage = stage;
            ArtifactId = artifactId;
            ShaderName = shaderName;
            AssetPath = assetPath;
            Source = source;
            Includes = includes ?? throw new ArgumentNullException(nameof(includes));
        }
    }
}