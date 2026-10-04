using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleNoiseParameter
    {
        Strength,
        Scale,
        Speed,

        Count
    }

    /// <summary>
    /// ParticleのNoiseに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleNoiseParameters : DataList
    {
        internal static FluxParticleNoiseParameters New(float strength, float scale, float speed)
        {
            var data = new DataToken[(int)FluxParticleNoiseParameter.Count];

            data[(int)FluxParticleNoiseParameter.Strength] = Mathf.Max(0, strength);
            data[(int)FluxParticleNoiseParameter.Scale] = Mathf.Max(0, scale);
            data[(int)FluxParticleNoiseParameter.Speed] = Mathf.Max(0, speed);

            return (FluxParticleNoiseParameters)new DataList(data);
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
        /// <param name="parameters">Noiseパラメーター</param>
        /// <returns>Noiseの強度</returns>
        [PublicAPI]
        public static float GetStrength(this FluxParticleNoiseParameters parameters)
        {
            return (float)parameters[(int)FluxParticleNoiseParameter.Strength].Double;
        }

        /// <summary>
        /// Noiseの強度を設定します。
        /// </summary>
        /// <param name="parameters">Noiseパラメーター</param>
        /// <param name="strength">Noiseの強度</param>
        [PublicAPI]
        public static void SetStrength(this FluxParticleNoiseParameters parameters, float strength)
        {
            parameters[(int)FluxParticleNoiseParameter.Strength] = Mathf.Max(0, strength);
        }

        /// <summary>
        /// Noiseのスケールを取得します。
        /// </summary>
        /// <param name="parameters">Noiseパラメーター</param>
        /// <returns>Noiseのスケール</returns>
        [PublicAPI]
        public static float GetScale(this FluxParticleNoiseParameters parameters)
        {
            return (float)parameters[(int)FluxParticleNoiseParameter.Scale].Double;
        }

        /// <summary>
        /// Noiseのスケールを設定します。
        /// </summary>
        /// <param name="parameters">Noiseパラメーター</param>
        /// <param name="scale">Noiseのスケール</param>
        [PublicAPI]
        public static void SetScale(this FluxParticleNoiseParameters parameters, float scale)
        {
            parameters[(int)FluxParticleNoiseParameter.Scale] = Mathf.Max(0, scale);
        }

        /// <summary>
        /// Noiseの時間変化速度を取得します。
        /// </summary>
        /// <param name="parameters">Noiseパラメーター</param>
        /// <returns>Noiseの時間変化速度</returns>
        [PublicAPI]
        public static float GetSpeed(this FluxParticleNoiseParameters parameters)
        {
            return (float)parameters[(int)FluxParticleNoiseParameter.Speed].Double;
        }

        /// <summary>
        /// Noiseの時間変化速度を設定します。
        /// </summary>
        /// <param name="parameters">Noiseパラメーター</param>
        /// <param name="speed">Noiseの時間変化速度</param>
        [PublicAPI]
        public static void SetSpeed(this FluxParticleNoiseParameters parameters, float speed)
        {
            parameters[(int)FluxParticleNoiseParameter.Speed] = Mathf.Max(0, speed);
        }
    }
}