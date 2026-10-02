using System.IO;
using TpLab.Flux.FX.Scripts;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor
{
    public class FluxParticleLutBaker
    {
        const int LutResolution = 64;
        const string GeneratedDirectory = "Assets/FluxFX/Generated";

        public Texture2D BakeColorOverLifetime(FluxParticleAuthoring authoring)
        {
            var settings = authoring.Render.ColorOverLifetime;
            if (!settings.Enabled || settings.Mode != FluxParticleColorOverLifetimeMode.Gradient)
            {
                DeleteLut(authoring, "ColorOverLifetime");
                return null;
            }

            var texture = CreateLut(authoring, "ColorOverLifetime");
            if (texture == null) return null;

            UpdateColorOverLifetimeLut(texture, settings);

            EditorUtility.SetDirty(texture);

            return texture;
        }

        public Texture2D BakeSizeOverLifetime(FluxParticleAuthoring authoring)
        {
            var settings = authoring.Render.SizeOverLifetime;
            if (!settings.Enabled || settings.Mode != FluxParticleSizeOverLifetimeMode.Curve)
            {
                DeleteLut(authoring, "SizeOverLifetime");
                return null;
            }

            var texture = CreateLut(authoring, "SizeOverLifetime");
            if (texture == null) return null;

            UpdateSizeOverLifetimeLut(texture, settings);

            EditorUtility.SetDirty(texture);

            return texture;
        }

        public Texture2D BakeRotationOverLifetime(FluxParticleAuthoring authoring)
        {
            var settings = authoring.Render.RotationOverLifetime;
            if (!settings.Enabled || settings.Mode != FluxParticleRotationOverLifetimeMode.Curve)
            {
                DeleteLut(authoring, "RotationOverLifetime");
                return null;
            }

            var texture = CreateLut(authoring, "RotationOverLifetime");
            if (texture == null) return null;

            UpdateRotationOverLifetimeLut(texture, settings);

            EditorUtility.SetDirty(texture);

            return texture;
        }

        public Texture2D CreatePreviewLut(string name)
        {
            var texture = new Texture2D(LutResolution, 1, TextureFormat.RGBAHalf, false, true);
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }

        public void UpdateColorOverLifetimeLut(Texture2D texture, ColorOverLifetimeSettings settings)
        {
            for (var i = 0; i < LutResolution; i++)
            {
                var normalizedAge = i / (float)(LutResolution - 1);
                texture.SetPixel(i, 0, settings.Gradient.Evaluate(normalizedAge));
            }

            texture.Apply(false, false);
        }

        public void UpdateSizeOverLifetimeLut(Texture2D texture, SizeOverLifetimeSettings settings)
        {
            for (var i = 0; i < LutResolution; i++)
            {
                var normalizedAge = i / (float)(LutResolution - 1);
                var sizeMultiplier = settings.Curve.Evaluate(normalizedAge);

                texture.SetPixel(i, 0, new Color(sizeMultiplier, 0, 0, 1));
            }

            texture.Apply(false, false);
        }

        public void UpdateRotationOverLifetimeLut(Texture2D texture, RotationOverLifetimeSettings settings)
        {
            for (var i = 0; i < LutResolution; i++)
            {
                var normalizedAge = i / (float)(LutResolution - 1);

                texture.SetPixel(i, 0, new Color(settings.Curve.Evaluate(normalizedAge), 0, 0, 1));
            }

            texture.Apply(false, false);
        }

        Texture2D CreateLut(FluxParticleAuthoring authoring, string type)
        {
            EnsureGeneratedDirectory();

            var path = GetLutPath(authoring, type);
            if (string.IsNullOrEmpty(path)) return null;

            var existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (existingTexture != null)
            {
                return existingTexture;
            }

            var texture = new Texture2D(LutResolution, 1, TextureFormat.RGBAHalf, false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(texture, path);

            return texture;
        }

        void DeleteLut(FluxParticleAuthoring authoring, string type)
        {
            var path = GetLutPath(authoring, type);
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.DeleteAsset(path);
        }

        void EnsureGeneratedDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FluxFX"))
            {
                AssetDatabase.CreateFolder("Assets", "FluxFX");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedDirectory))
            {
                AssetDatabase.CreateFolder("Assets/FluxFX", "Generated");
            }
        }

        string GetLutPath(FluxParticleAuthoring authoring, string type)
        {
            var scenePath = authoring.gameObject.scene.path;

            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError(
                    $"[FluxFX] Cannot generate {type} LUT because '{authoring.name}' is not in a saved scene.",
                    authoring);
                return string.Empty;
            }

            var identifier = GlobalObjectId.GetGlobalObjectIdSlow(authoring).ToString();

            return $"{GeneratedDirectory}/{identifier}_{type}.asset";
        }
    }
}