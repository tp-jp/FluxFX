using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderIncludeArtifactStore
    {
        public void Save(ShaderIncludeArtifactSet artifactSet)
        {
            if (artifactSet == null) throw new ArgumentNullException(nameof(artifactSet));

            var pending = new List<ShaderIncludeArtifact>();
            var paths = new Dictionary<string, string>(StringComparer.Ordinal);

            // 保存前に全Artifactを検証し、既存ファイルを上書きしない。
            foreach (var artifact in artifactSet.Artifacts)
            {
                if (paths.TryGetValue(artifact.AssetPath, out var existingSource))
                {
                    if (existingSource != artifact.Source)
                    {
                        throw new InvalidOperationException($"Conflicting Include artifacts: {artifact.AssetPath}");
                    }

                    continue;
                }

                paths.Add(artifact.AssetPath, artifact.Source);

                if (!File.Exists(artifact.AssetPath))
                {
                    pending.Add(artifact);
                    continue;
                }

                var currentSource = File.ReadAllText(artifact.AssetPath);

                if (currentSource != artifact.Source)
                {
                    throw new InvalidOperationException($"Generated Include artifact was modified: {artifact.AssetPath}");
                }
            }

            foreach (var artifact in pending)
            {
                var directory = Path.GetDirectoryName(artifact.AssetPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 生成済みの同名ファイルは上書きしない。
                using (var stream = new FileStream(artifact.AssetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(artifact.Source);
                }

                AssetDatabase.ImportAsset(artifact.AssetPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}