using System.Linq;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifactAutoCleanupExecutor
    {
        public void Execute()
        {
            var cleanup = new ShaderArtifactCleanup();
            var manifest = ShaderArtifactManifest.Load();

            var report = cleanup.Analyze();

            var shaderCandidates = report.ShaderCandidates
                .Where(manifest.CanDelete)
                .ToArray();

            var deletedShaders = 0;
            var deletedIncludes = 0;

            // 削除直前にも既存のCleanupで候補を再検証する。
            foreach (var path in shaderCandidates)
            {
                if (!manifest.CanDelete(path)) continue;

                var current = cleanup.Analyze();
                if (!current.ShaderCandidates.Contains(path)) continue;

                deletedShaders += cleanup.DeleteShaders(new[] { path });
                manifest.Unregister(new[] { path });
            }

            // Shader削除後の依存関係を改めて調査する。
            report = cleanup.Analyze();

            var includeCandidates = report.IncludeCandidates
                .Where(manifest.CanDelete)
                .ToArray();

            foreach (var path in includeCandidates)
            {
                if (!manifest.CanDelete(path)) continue;

                var current = cleanup.Analyze();
                if (!current.IncludeCandidates.Contains(path)) continue;

                deletedIncludes += cleanup.DeleteIncludes(new[] { path });
                manifest.Unregister(new[] { path });
            }

            AssetDatabase.SaveAssets();

            Logger.Log(
                $"Automatic Shader Cleanup completed. " +
                $"Shaders: {deletedShaders}, Includes: {deletedIncludes}");
        }
    }
}