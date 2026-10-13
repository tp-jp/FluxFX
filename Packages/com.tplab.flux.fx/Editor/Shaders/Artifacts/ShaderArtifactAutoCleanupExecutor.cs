using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Build;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifactAutoCleanupExecutor
    {
        public void Execute(FluxFXCompilationSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var cleanup = new ShaderArtifactCleanup();
            var manifest = ShaderArtifactManifest.Load();
            var protectedPaths = CollectProtectedPaths(session);

            var report = cleanup.Analyze(protectedPaths);

            var shaderCandidates = report.ShaderCandidates
                .Where(manifest.CanDelete)
                .ToArray();

            var deletedShaders = 0;
            var deletedIncludes = 0;

            foreach (var path in shaderCandidates)
            {
                if (!manifest.CanDelete(path)) continue;

                var current = cleanup.Analyze(protectedPaths);
                if (!current.ShaderCandidates.Contains(path)) continue;

                deletedShaders += cleanup.DeleteShaders(new[] { path }, protectedPaths);
                manifest.Unregister(new[] { path });
            }

            // Shader削除後にIncludeの依存関係を再評価する。
            report = cleanup.Analyze(protectedPaths);

            var includeCandidates = report.IncludeCandidates
                .Where(manifest.CanDelete)
                .ToArray();

            foreach (var path in includeCandidates)
            {
                if (!manifest.CanDelete(path)) continue;

                var current = cleanup.Analyze(protectedPaths);
                if (!current.IncludeCandidates.Contains(path)) continue;

                deletedIncludes += cleanup.DeleteIncludes(new[] { path }, protectedPaths);
                manifest.Unregister(new[] { path });
            }

            AssetDatabase.SaveAssets();

            Logger.Log(
                $"Automatic Shader Cleanup completed. " +
                $"Shaders: {deletedShaders}, Includes: {deletedIncludes}");
        }

        static HashSet<string> CollectProtectedPaths(FluxFXCompilationSession session)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var result in session.Results)
            {
                foreach (var artifact in result.ShaderArtifacts.Values)
                {
                    paths.Add(artifact.AssetPath);

                    foreach (var include in artifact.Includes.Artifacts)
                    {
                        paths.Add(include.AssetPath);
                    }
                }
            }

            return paths;
        }
    }
}
