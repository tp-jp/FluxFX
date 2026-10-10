using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifactCleanupReport
    {
        public IReadOnlyList<string> ShaderCandidates { get; }
        public IReadOnlyList<string> IncludeCandidates { get; }
        public int ProtectedShaderCount { get; }
        public int ProtectedIncludeCount { get; }

        public ShaderArtifactCleanupReport(List<string> shaders, List<string> includes, int protectedShaders, int protectedIncludes)
        {
            ShaderCandidates = shaders.AsReadOnly();
            IncludeCandidates = includes.AsReadOnly();
            ProtectedShaderCount = protectedShaders;
            ProtectedIncludeCount = protectedIncludes;
        }
    }

    public sealed class ShaderArtifactCleanup
    {
        const string ShaderDirectory = "Assets/FluxFX/Generated/Shaders";
        const string IncludeDirectory = ShaderIncludeArtifactBuilder.GeneratedDirectory;

        static readonly Regex ShaderNamePattern = new Regex(@"^[A-Za-z][A-Za-z0-9]*_[a-f0-9]{64}\.shader$", RegexOptions.Compiled);
        static readonly Regex IncludeNamePattern = new Regex(@"^.+_[a-f0-9]{64}\.(?:hlsl|cginc)$", RegexOptions.Compiled);
        static readonly Regex GeneratedIncludePattern = new Regex(
            @"#\s*include\s*""(?<path>Assets/FluxFX/Generated/Includes/[^""]+)""",
            RegexOptions.Compiled);

        public ShaderArtifactCleanupReport Analyze()
        {
            EnsureScenesSaved();

            var shaders = EnumerateGenerated(ShaderDirectory, "*.shader", ShaderNamePattern);
            var includes = EnumerateGenerated(IncludeDirectory, "*.*", IncludeNamePattern);

            var includePaths = new HashSet<string>(includes, StringComparer.Ordinal);
            var protectedPaths = CollectProjectDependencies();

            ProtectCurrentAuthoring(protectedPaths);

            var protectedShaders = new HashSet<string>(shaders.Where(protectedPaths.Contains), StringComparer.Ordinal);
            var protectedIncludes = new HashSet<string>(includes.Where(protectedPaths.Contains), StringComparer.Ordinal);

            foreach (var shader in protectedShaders)
            {
                ProtectIncludes(shader, includePaths, protectedIncludes, new HashSet<string>(StringComparer.Ordinal), true);
            }

            foreach (var include in protectedIncludes.ToArray())
            {
                ProtectIncludes(include, includePaths, protectedIncludes, new HashSet<string>(StringComparer.Ordinal), true);
            }

            // 候補Shaderが使用しているIncludeは、Shaderの削除成功後に削除可能になる。
            // ここでは直接参照がないIncludeのみを候補にする。
            foreach (var shader in shaders)
            {
                if (protectedShaders.Contains(shader)) continue;

                ProtectIncludes(shader, includePaths, protectedIncludes, new HashSet<string>(StringComparer.Ordinal), false);
            }

            var shaderCandidates = shaders.Where(path => !protectedShaders.Contains(path)).ToList();
            var includeCandidates = includes.Where(path => !protectedIncludes.Contains(path)).ToList();

            return new ShaderArtifactCleanupReport(shaderCandidates, includeCandidates, protectedShaders.Count, protectedIncludes.Count);
        }

        public int DeleteShaders(IEnumerable<string> selectedPaths)
        {
            return DeleteCandidates(selectedPaths, report => report.ShaderCandidates);
        }

        public int DeleteIncludes(IEnumerable<string> selectedPaths)
        {
            return DeleteCandidates(selectedPaths, report => report.IncludeCandidates);
        }

        int DeleteCandidates(IEnumerable<string> selectedPaths, Func<ShaderArtifactCleanupReport, IReadOnlyList<string>> getCandidates)
        {
            if (selectedPaths == null) throw new ArgumentNullException(nameof(selectedPaths));

            var report = Analyze();
            var allowed = new HashSet<string>(getCandidates(report), StringComparer.Ordinal);
            var paths = selectedPaths.Distinct(StringComparer.Ordinal).ToArray();

            foreach (var path in paths)
            {
                if (!allowed.Contains(path))
                {
                    throw new InvalidOperationException($"Shader cleanup candidate is no longer safe to delete: {path}");
                }
            }

            var count = 0;

            foreach (var path in paths)
            {
                if (!AssetDatabase.DeleteAsset(path))
                {
                    throw new IOException($"Failed to delete generated Shader artifact: {path}");
                }

                count++;
            }

            return count;
        }

        static List<string> EnumerateGenerated(string directory, string pattern, Regex namePattern)
        {
            if (!Directory.Exists(directory)) return new List<string>();

            return Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => namePattern.IsMatch(Path.GetFileName(path)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        static HashSet<string> CollectProjectDependencies()
        {
            var externalAssets = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal))
                .Where(path => !path.StartsWith("Assets/FluxFX/Generated/", StringComparison.Ordinal))
                .ToArray();

            var dependencies = AssetDatabase.GetDependencies(externalAssets, true);

            return new HashSet<string>(dependencies.Select(path => path.Replace('\\', '/')), StringComparer.Ordinal);
        }

        static void ProtectCurrentAuthoring(HashSet<string> protectedPaths)
        {
            var registry = new FluxParticleGpuModuleRegistry();
            var pipeline = new FluxParticleShaderPipeline(registry);
            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);

            foreach (var authoring in authorings)
            {
                var plan = FluxParticleCompilePlan.Create(authoring);
                var artifact = pipeline.BuildVelocity(plan);

                protectedPaths.Add(artifact.AssetPath);

                foreach (var include in artifact.Includes.Artifacts)
                {
                    protectedPaths.Add(include.AssetPath);
                }
            }
        }

        static void ProtectIncludes(string sourcePath, HashSet<string> availableIncludes, HashSet<string> protectedIncludes,
            HashSet<string> visiting, bool requireAvailable)
        {
            if (!visiting.Add(sourcePath)) return;

            var source = File.ReadAllText(sourcePath);

            foreach (Match match in GeneratedIncludePattern.Matches(source))
            {
                var path = match.Groups["path"].Value;

                if (!availableIncludes.Contains(path))
                {
                    if (requireAvailable)
                    {
                        throw new InvalidOperationException($"Protected Shader dependency is missing or unrecognized: {path}");
                    }

                    continue;
                }

                protectedIncludes.Add(path);
                ProtectIncludes(path, availableIncludes, protectedIncludes, visiting, requireAvailable);
            }
        }

        static void EnsureScenesSaved()
        {
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);

                if (scene.isDirty)
                {
                    throw new InvalidOperationException($"Save the modified scene before Shader cleanup: {scene.name}");
                }
            }
        }
    }
}