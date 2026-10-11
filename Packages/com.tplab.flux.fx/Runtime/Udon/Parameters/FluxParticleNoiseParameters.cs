using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// ParticleのNoiseに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleNoiseParameters : FluxParticleModuleParameters
    {
        internal static FluxParticleNoiseParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (FluxParticleNoiseParameters)data;
        }
    }

    /// <summary>
    /// FluxParticleNoiseParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleNoiseParametersExtensions
    {
        /// <summary>
        /// Noiseの強度を取得します。
        /// </summary>
        [PublicAPI]
        public static float GetStrength(this FluxParticleNoiseParameters parameters)
        {
            return parameters.GetFloat("_NoiseStrength");
        }

        /// <summary>
        /// Noiseの強度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetStrength(this FluxParticleNoiseParameters parameters, float strength)
        {
            parameters.SetFloat("_NoiseStrength", Mathf.Max(0, strength));
        }

        /// <summary>
        /// Noiseのスケールを取得します。
        /// </summary>
        [PublicAPI]
        public static float GetScale(this FluxParticleNoiseParameters parameters)
        {
            return parameters.GetFloat("_NoiseScale");
        }

        /// <summary>
        /// Noiseのスケールを設定します。
        /// </summary>
        [PublicAPI]
        public static void SetScale(this FluxParticleNoiseParameters parameters, float scale)
        {
            parameters.SetFloat("_NoiseScale", Mathf.Max(0, scale));
        }

        /// <summary>
        /// Noiseの時間変化速度を取得します。
        /// </summary>
        [PublicAPI]
        public static float GetSpeed(this FluxParticleNoiseParameters parameters)
        {
            return parameters.GetFloat("_NoiseSpeed");
        }

        /// <summary>
        /// Noiseの時間変化速度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetSpeed(this FluxParticleNoiseParameters parameters, float speed)
        {
            parameters.SetFloat("_NoiseSpeed", Mathf.Max(0, speed));
        }
    }
}