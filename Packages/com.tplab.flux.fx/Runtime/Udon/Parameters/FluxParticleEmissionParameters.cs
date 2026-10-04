using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleEmissionParameter
    {
        Rate,

        Count
    }

    /// <summary>
    /// ParticleのEmissionに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleEmissionParameters : DataList
    {
        internal static FluxParticleEmissionParameters New(float rate)
        {
            var data = new DataToken[(int)FluxParticleEmissionParameter.Count];

            data[(int)FluxParticleEmissionParameter.Rate] = Mathf.Max(0, rate);

            return (FluxParticleEmissionParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleEmissionParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleEmissionParametersExt
    {
        /// <summary>
        /// 1秒あたりのParticle生成数を取得します。
        /// </summary>
        /// <param name="parameters">Emissionパラメーター</param>
        /// <returns>1秒あたりのParticle生成数</returns>
        [PublicAPI]
        public static float GetRate(this FluxParticleEmissionParameters parameters)
        {
            return (float)parameters[(int)FluxParticleEmissionParameter.Rate].Double;
        }

        /// <summary>
        /// 1秒あたりのParticle生成数を設定します。
        /// </summary>
        /// <param name="parameters">Emissionパラメーター</param>
        /// <param name="rate">1秒あたりのParticle生成数</param>
        [PublicAPI]
        public static void SetRate(this FluxParticleEmissionParameters parameters, float rate)
        {
            parameters[(int)FluxParticleEmissionParameter.Rate] = Mathf.Max(0, rate);
        }
    }
}