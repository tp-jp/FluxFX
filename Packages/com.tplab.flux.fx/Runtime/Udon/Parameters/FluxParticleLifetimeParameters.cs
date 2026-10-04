using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleLifetimeParameter
    {
        Min,
        Max,

        Count
    }

    /// <summary>
    /// ParticleのLifetimeに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleLifetimeParameters : DataList
    {
        internal static FluxParticleLifetimeParameters New(float min, float max)
        {
            var data = new DataToken[(int)FluxParticleLifetimeParameter.Count];

            data[(int)FluxParticleLifetimeParameter.Min] = Mathf.Max(0, min);
            data[(int)FluxParticleLifetimeParameter.Max] = Mathf.Max(0, max);

            return (FluxParticleLifetimeParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleLifetimeParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleLifetimeParametersExtensions
    {
        /// <summary>
        /// Lifetimeの最小値を取得します。
        /// </summary>
        /// <param name="parameters">Lifetimeパラメーター</param>
        /// <returns>Lifetimeの最小値</returns>
        [PublicAPI]
        public static float GetMin(this FluxParticleLifetimeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleLifetimeParameter.Min].Double;
        }

        /// <summary>
        /// Lifetimeの最小値を設定します。
        /// </summary>
        /// <param name="parameters">Lifetimeパラメーター</param>
        /// <param name="min">Lifetimeの最小値</param>
        [PublicAPI]
        public static void SetMin(this FluxParticleLifetimeParameters parameters, float min)
        {
            parameters[(int)FluxParticleLifetimeParameter.Min] = Mathf.Max(0, min);
        }

        /// <summary>
        /// Lifetimeの最大値を取得します。
        /// </summary>
        /// <param name="parameters">Lifetimeパラメーター</param>
        /// <returns>Lifetimeの最大値</returns>
        [PublicAPI]
        public static float GetMax(this FluxParticleLifetimeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleLifetimeParameter.Max].Double;
        }

        /// <summary>
        /// Lifetimeの最大値を設定します。
        /// </summary>
        /// <param name="parameters">Lifetimeパラメーター</param>
        /// <param name="max">Lifetimeの最大値</param>
        [PublicAPI]
        public static void SetMax(this FluxParticleLifetimeParameters parameters, float max)
        {
            parameters[(int)FluxParticleLifetimeParameter.Max] = Mathf.Max(0, max);
        }
    }
}