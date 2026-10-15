using System;
using System.Linq;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Shared
{
    /// <summary>
    /// Gradientを補間方式に応じたLookupサンプルへ変換します。
    /// </summary>
    public static class FluxParticleGradientBaker
    {
        /// <summary>
        /// Blendは等間隔、Fixedはキー位置に対応する区間の値を取得します。
        /// </summary>
        /// <param name="gradient">評価するGradient</param>
        /// <param name="positions">Fixedの区間終端。Blendでは空配列</param>
        /// <returns>RGBAサンプル</returns>
        public static Vector4[] Bake(Gradient gradient, out float[] positions)
        {
            if (gradient == null || (gradient.mode != GradientMode.Blend && gradient.mode != GradientMode.Fixed))
            {
                throw new InvalidOperationException("Unsupported color gradient mode.");
            }
            positions = gradient.mode == GradientMode.Fixed
                ? gradient.colorKeys.Select(key => key.time).Concat(gradient.alphaKeys.Select(key => key.time))
                    .Concat(new[] { 0f, 1f }).Distinct().OrderBy(time => time).ToArray()
                : Array.Empty<float>();
            var samples = new Vector4[positions.Length != 0 ? positions.Length : 128];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = positions.Length == 0 ? i / (float)(samples.Length - 1)
                    : i == 0 ? positions[0] : (positions[i - 1] + positions[i]) * 0.5f;
                samples[i] = gradient.Evaluate(time);
            }
            return samples;
        }
    }
}
