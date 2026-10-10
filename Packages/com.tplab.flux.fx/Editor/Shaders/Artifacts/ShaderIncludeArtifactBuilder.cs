using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TpLab.Flux.FX.Editor.Shaders.Dependencies;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderIncludeArtifactBuilder
    {
        public const string GeneratedDirectory = "Assets/FluxFX/Generated/Includes";

        readonly string _generatedDirectory;
        readonly Dictionary<string, ShaderIncludeArtifact> _artifacts = new Dictionary<string, ShaderIncludeArtifact>(StringComparer.Ordinal);

        public ShaderIncludeArtifactBuilder(string generatedDirectory = GeneratedDirectory)
        {
            _generatedDirectory = generatedDirectory.TrimEnd('/').Replace('\\', '/');

            if (!_generatedDirectory.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Generated Include directory must be inside Assets.", nameof(generatedDirectory));
            }
        }

        public ShaderIncludeArtifactSet Build(ShaderDependencyGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            _artifacts.Clear();

            foreach (var root in graph.Roots)
            {
                BuildSource(graph, root);
            }

            var artifacts = new List<ShaderIncludeArtifact>(_artifacts.Values);
            artifacts.Sort((a, b) => string.CompareOrdinal(a.SourcePath, b.SourcePath));

            return new ShaderIncludeArtifactSet(artifacts);
        }

        ShaderIncludeArtifact BuildSource(ShaderDependencyGraph graph, string path)
        {
            if (_artifacts.TryGetValue(path, out var cached)) return cached;

            var dependency = graph.GetSource(path);
            var source = dependency.Source;
            var replacements = new List<(int start, int length, string value)>();

            foreach (var include in dependency.Includes)
            {
                if (include.IsExternal) continue;

                var child = BuildSource(graph, include.Path);
                replacements.Add((include.StartIndex, include.Length, child.AssetPath));
            }

            // 後方から置換することで、元ソースの文字位置を維持する。
            replacements.Sort((a, b) => b.start.CompareTo(a.start));

            foreach (var replacement in replacements)
            {
                source = source.Substring(0, replacement.start) +
                         replacement.value +
                         source.Substring(replacement.start + replacement.length);
            }

            var hash = ComputeHash(source);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);

            var assetPath = $"{_generatedDirectory}/{fileName}_{hash}{extension}";
            var artifact = new ShaderIncludeArtifact(path, assetPath, hash, source);

            _artifacts.Add(path, artifact);

            return artifact;
        }

        static string ComputeHash(string source)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}