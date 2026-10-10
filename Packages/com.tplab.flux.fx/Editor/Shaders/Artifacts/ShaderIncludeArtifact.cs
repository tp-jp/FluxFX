using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderIncludeArtifact
    {
        public string SourcePath { get; }
        public string AssetPath { get; }
        public string Hash { get; }
        public string Source { get; }

        public ShaderIncludeArtifact(string sourcePath, string assetPath, string hash, string source)
        {
            SourcePath = sourcePath;
            AssetPath = assetPath;
            Hash = hash;
            Source = source;
        }
    }

    public sealed class ShaderIncludeArtifactSet
    {
        public IReadOnlyList<ShaderIncludeArtifact> Artifacts { get; }
        public IReadOnlyDictionary<string, ShaderIncludeArtifact> BySourcePath { get; }

        public ShaderIncludeArtifactSet(IList<ShaderIncludeArtifact> artifacts)
        {
            Artifacts = new ReadOnlyCollection<ShaderIncludeArtifact>(new List<ShaderIncludeArtifact>(artifacts));

            var bySourcePath = new Dictionary<string, ShaderIncludeArtifact>(StringComparer.Ordinal);

            foreach (var artifact in artifacts)
            {
                bySourcePath.Add(artifact.SourcePath, artifact);
            }

            BySourcePath = new ReadOnlyDictionary<string, ShaderIncludeArtifact>(bySourcePath);
        }

        public ShaderIncludeArtifact Get(string sourcePath)
        {
            if (BySourcePath.TryGetValue(sourcePath, out var artifact)) return artifact;

            throw new InvalidOperationException($"Shader include artifact was not found: {sourcePath}");
        }
    }
}