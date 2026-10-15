using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 固定LUTの宣言から補間方式に応じたTextureを生成します。
    /// </summary>
    public static class FluxParticleLookupTexture
    {
        /// <summary>
        /// サンプルを線形色空間の1行Textureへ格納します。
        /// </summary>
        /// <param name="provider">固定LUTのProvider宣言</param>
        /// <returns>呼び出し元が所有するLUT Texture</returns>
        public static Texture2D Create(FluxParticleValueProvider provider)
        {
            if (provider.Kind != FluxParticleProviderKind.LookupTable)
            {
                throw new ArgumentException("A LookupTable provider is required.", nameof(provider));
            }
            var count = provider.LookupSampleCount;
            var texture = new Texture2D(count, 1, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = provider.LookupPositions.Count == 0 ? FilterMode.Bilinear : FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };
            var colors = new Color[count];
            for (var i = 0; i < count; i++)
            {
                colors[i] = provider.LookupVectorSamples.Count != 0 ? (Color)provider.LookupVectorSamples[i] : new Color(provider.LookupSamples[i], 0, 0, 0);
            }
            texture.SetPixels(colors);
            texture.Apply(false, false);
            return texture;
        }
    }
}
