using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleStartSizeParameter
    {
        Min,
        Max,

        Count
    }

    /// <summary>
    /// ParticleのStart Sizeに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleStartSizeParameters : DataList
    {
        internal static FluxParticleStartSizeParameters New(float min, float max)
        {
            var data = new DataToken[(int)FluxParticleStartSizeParameter.Count];

            data[(int)FluxParticleStartSizeParameter.Min] = Mathf.Max(0, min);
            data[(int)FluxParticleStartSizeParameter.Max] = Mathf.Max(0, max);

            return (FluxParticleStartSizeParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleStartSizeParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleStartSizeParametersExtensions
    {
        /// <summary>
        /// Start Sizeの最小値を取得します。
        /// </summary>
        /// <param name="parameters">Start Sizeパラメーター</param>
        /// <returns>Start Sizeの最小値</returns>
        [PublicAPI]
        public static float GetMin(this FluxParticleStartSizeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleStartSizeParameter.Min].Double;
        }

        /// <summary>
        /// Start Sizeの最小値を設定します。
        /// </summary>
        /// <param name="parameters">Start Sizeパラメーター</param>
        /// <param name="min">Start Sizeの最小値</param>
        [PublicAPI]
        public static void SetMin(this FluxParticleStartSizeParameters parameters, float min)
        {
            parameters[(int)FluxParticleStartSizeParameter.Min] = Mathf.Max(0, min);
        }

        /// <summary>
        /// Start Sizeの最大値を取得します。
        /// </summary>
        /// <param name="parameters">Start Sizeパラメーター</param>
        /// <returns>Start Sizeの最大値</returns>
        [PublicAPI]
        public static float GetMax(this FluxParticleStartSizeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleStartSizeParameter.Max].Double;
        }

        /// <summary>
        /// Start Sizeの最大値を設定します。
        /// </summary>
        /// <param name="parameters">Start Sizeパラメーター</param>
        /// <param name="max">Start Sizeの最大値</param>
        [PublicAPI]
        public static void SetMax(this FluxParticleStartSizeParameters parameters, float max)
        {
            parameters[(int)FluxParticleStartSizeParameter.Max] = Mathf.Max(0, max);
        }
    }
}