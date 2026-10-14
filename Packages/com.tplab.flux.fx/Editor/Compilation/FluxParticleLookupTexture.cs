using System;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 固定LUTの宣言から線形補間用Textureを生成します。
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
            var samples = provider.LookupSamples;
            var texture = new Texture2D(samples.Count, 1, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };
            var colors = new Color[samples.Count];
            for (var i = 0; i < samples.Count; i++)
            {
                colors[i] = new Color(samples[i], 0, 0, 0);
            }
            texture.SetPixels(colors);
            texture.Apply(false, false);
            return texture;
        }
    }
}
