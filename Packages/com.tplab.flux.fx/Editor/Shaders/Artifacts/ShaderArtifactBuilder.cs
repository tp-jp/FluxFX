using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TpLab.Flux.FX.Editor.Shaders.Dependencies;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifactBuilder
    {
        const int ArtifactFormatVersion = 1;
        const string ShaderNameSlot = "{{SHADER_NAME}}";
        const string GeneratedDirectory = "Assets/FluxFX/Generated/Shaders";

        static readonly Regex IncludePattern = new Regex(
            "^[ \\t]*#[ \\t]*include[ \\t]+\"(?<path>[^\"]+)\"[ \\t]*(?://[^\\r\\n]*)?$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        static readonly Regex IncludeDirectivePattern = new Regex(
            @"^[ \t]*#[ \t]*include\b[^\r\n]*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        readonly ShaderDependencyResolver _resolver;
        readonly ShaderIncludeArtifactBuilder _includeBuilder;

        public ShaderArtifactBuilder()
        {
            _resolver = new ShaderDependencyResolver();
            _includeBuilder = new ShaderIncludeArtifactBuilder();
        }

        public ShaderArtifact Build(FluxParticleShaderDraft draft)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));

            var source = draft.Source;
            var roots = new List<string>();
            var replacements = new List<(int start, int length, string path)>();

            foreach (Match directive in IncludeDirectivePattern.Matches(source))
            {
                var match = IncludePattern.Match(source, directive.Index);

                if (!match.Success || match.Index != directive.Index || match.Length != directive.Length)
                {
                    throw new InvalidOperationException($"Unsupported Shader include directive: {directive.Value.Trim()}");
                }

                var group = match.Groups["path"];
                var path = group.Value.Replace('\\', '/');

                if (IsProjectInclude(path))
                {
                    if (!roots.Contains(path))
                    {
                        roots.Add(path);
                    }

                    replacements.Add((group.Index, group.Length, path));
                    continue;
                }

                if (!IsExternalInclude(path))
                {
                    throw new InvalidOperationException($"Unsupported external Shader include: {path}");
                }
            }

            var graph = _resolver.Resolve(roots);
            var includes = _includeBuilder.Build(graph);

            replacements.Sort((a, b) => b.start.CompareTo(a.start));

            foreach (var replacement in replacements)
            {
                var includeAssetPath = includes.Get(replacement.path).AssetPath;

                source = source.Substring(0, replacement.start) +
                         includeAssetPath +
                         source.Substring(replacement.start + replacement.length);
            }

            if (source.IndexOf(ShaderNameSlot, StringComparison.Ordinal) < 0 ||
                source.IndexOf(ShaderNameSlot, source.IndexOf(ShaderNameSlot, StringComparison.Ordinal) + ShaderNameSlot.Length, StringComparison.Ordinal) >= 0)
            {
                throw new InvalidOperationException($"Shader name slot must appear exactly once: {ShaderNameSlot}");
            }

            var hashInput = ArtifactFormatVersion + "\n" +
                            Application.unityVersion + "\n" +
                            draft.Stage + "\n" +
                            source;

            var artifactId = ComputeHash(hashInput);
            var stageName = draft.Stage == Scripts.Modules.FluxParticleExecutionStage.VelocityUpdate ? "Velocity" : draft.Stage.ToString();
            var shaderName = $"Hidden/FluxFX/Generated/{stageName}_{artifactId}";
            var assetPath = $"{GeneratedDirectory}/{stageName}_{artifactId}.shader";

            source = source.Replace(ShaderNameSlot, shaderName);

            return new ShaderArtifact(draft.Stage, artifactId, shaderName, assetPath, source, includes);
        }

        static bool IsProjectInclude(string path)
        {
            return path.StartsWith("Assets/", StringComparison.Ordinal) ||
                   path.StartsWith("Packages/", StringComparison.Ordinal);
        }

        static bool IsExternalInclude(string path)
        {
            switch (path)
            {
                case "UnityCG.cginc":
                case "UnityShaderVariables.cginc":
                case "UnityInstancing.cginc":
                case "AutoLight.cginc":
                case "Lighting.cginc":
                case "UnityPBSLighting.cginc":
                case "HLSLSupport.cginc":
                    return true;

                default:
                    return false;
            }
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