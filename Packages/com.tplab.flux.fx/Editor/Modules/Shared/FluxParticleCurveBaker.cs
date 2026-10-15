using System;
using System.Linq;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Shared
{
    /// <summary>
    /// AnimationCurveを固定LUT用のサンプルへ変換します。
    /// </summary>
    public static class FluxParticleCurveBaker
    {
        /// <summary>
        /// 0から1の区間を128点でサンプリングします。
        /// </summary>
        /// <param name="curve">評価するCurve</param>
        /// <returns>有限値のLUTサンプル</returns>
        public static float[] Bake(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0 || curve.keys.Any(key =>
                float.IsNaN(key.time) || float.IsInfinity(key.time) || float.IsNaN(key.value) || float.IsInfinity(key.value)))
            {
                throw new InvalidOperationException("Curve requires finite keys.");
            }
            const int count = 128;
            var samples = new float[count];
            for (var i = 0; i < count; i++)
            {
                var value = curve.Evaluate(i / (float)(count - 1));
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    throw new InvalidOperationException("Curve samples must be finite.");
                }
                samples[i] = value;
            }
            return samples;
        }
    }
}
