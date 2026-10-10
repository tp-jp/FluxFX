using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifactStore
    {
        readonly ShaderIncludeArtifactStore _includeStore = new ShaderIncludeArtifactStore();

        public Shader GetOrCreate(ShaderArtifact artifact)
        {
            if (artifact == null) throw new ArgumentNullException(nameof(artifact));

            // Shaderが参照する固定Includeを先に保存する。
            _includeStore.Save(artifact.Includes);

            var path = artifact.AssetPath;
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var created = false;

            if (File.Exists(path))
            {
                if (File.ReadAllText(path) != artifact.Source)
                {
                    throw new InvalidOperationException($"Generated Shader artifact was modified: {path}");
                }
            }
            else
            {
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(artifact.Source);
                }

                created = true;
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);

            if (created || shader == null || shader.name != artifact.ShaderName)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            }

            if (shader == null || shader.name != artifact.ShaderName || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
            {
                throw new InvalidOperationException($"Generated Shader is invalid or unsupported: {path}");
            }

            if (created)
            {
                Logger.Log($"Shader created: {path}");
            }
            else
            {
                Logger.Log($"Shader reused: {path}");
            }

            return shader;
        }
    }
}