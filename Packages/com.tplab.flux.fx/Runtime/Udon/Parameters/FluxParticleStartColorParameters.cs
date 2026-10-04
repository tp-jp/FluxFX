using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleStartColorParameter
    {
        Min,
        Max,

        Count
    }

    /// <summary>
    /// ParticleのStart Colorに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleStartColorParameters : DataList
    {
        internal static FluxParticleStartColorParameters New(Color min, Color max)
        {
            var data = new DataToken[(int)FluxParticleStartColorParameter.Count];

            data[(int)FluxParticleStartColorParameter.Min] = new DataToken(min);
            data[(int)FluxParticleStartColorParameter.Max] = new DataToken(max);

            return (FluxParticleStartColorParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleStartColorParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleStartColorParametersExtensions
    {
        /// <summary>
        /// Start Colorの最小値を取得します。
        /// </summary>
        /// <param name="parameters">Start Colorパラメーター</param>
        /// <returns>Start Colorの最小値</returns>
        [PublicAPI]
        public static Color GetMin(this FluxParticleStartColorParameters parameters)
        {
            return (Color)parameters[(int)FluxParticleStartColorParameter.Min].Reference;
        }

        /// <summary>
        /// Start Colorの最小値を設定します。
        /// </summary>
        /// <param name="parameters">Start Colorパラメーター</param>
        /// <param name="min">Start Colorの最小値</param>
        [PublicAPI]
        public static void SetMin(this FluxParticleStartColorParameters parameters, Color min)
        {
            parameters[(int)FluxParticleStartColorParameter.Min] = new DataToken(min);
        }

        /// <summary>
        /// Start Colorの最大値を取得します。
        /// </summary>
        /// <param name="parameters">Start Colorパラメーター</param>
        /// <returns>Start Colorの最大値</returns>
        [PublicAPI]
        public static Color GetMax(this FluxParticleStartColorParameters parameters)
        {
            return (Color)parameters[(int)FluxParticleStartColorParameter.Max].Reference;
        }

        /// <summary>
        /// Start Colorの最大値を設定します。
        /// </summary>
        /// <param name="parameters">Start Colorパラメーター</param>
        /// <param name="max">Start Colorの最大値</param>
        [PublicAPI]
        public static void SetMax(this FluxParticleStartColorParameters parameters, Color max)
        {
            parameters[(int)FluxParticleStartColorParameter.Max] = new DataToken(max);
        }
    }
}