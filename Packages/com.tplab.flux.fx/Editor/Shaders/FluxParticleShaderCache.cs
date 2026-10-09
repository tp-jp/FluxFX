using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Shaders
{
    public sealed class FluxParticleShaderCache
    {
        public const string GeneratedDirectory = "Assets/FluxFX/Generated/Shaders";

        public Shader GetOrCreate(FluxParticleShaderCompilation compilation)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));

            var stageName = compilation.Stage == Scripts.Modules.FluxParticleExecutionStage.VelocityUpdate
                ? "Velocity"
                : compilation.Stage.ToString();

            var path = $"{GeneratedDirectory}/{stageName}_{compilation.CacheKey}.shader";

            Directory.CreateDirectory(GeneratedDirectory);

            if (File.Exists(path))
            {
                var existingSource = File.ReadAllText(path);

                if (existingSource != compilation.Source)
                    throw new InvalidOperationException($"Shader cache collision or modified generated asset: {path}");

                var cachedShader = AssetDatabase.LoadAssetAtPath<Shader>(path);

                if (cachedShader != null && cachedShader.name == compilation.ShaderName)
                {
                    Logger.Log($"Shader reused: {path}");
                    return cachedShader;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            else
            {
                File.WriteAllText(path, compilation.Source);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Logger.Log($"Shader created: {path}");
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);

            if (shader == null || shader.name != compilation.ShaderName || !shader.isSupported)
                throw new InvalidOperationException($"Generated Shader is invalid or unsupported: {path}");

            return shader;
        }
    }
}
