using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleInitialVelocityParameter
    {
        Min,
        Max,

        Count
    }

    /// <summary>
    /// ParticleのInitial Velocityに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleInitialVelocityParameters : DataList
    {
        internal static FluxParticleInitialVelocityParameters New(float min, float max)
        {
            var data = new DataToken[(int)FluxParticleInitialVelocityParameter.Count];

            data[(int)FluxParticleInitialVelocityParameter.Min] = Mathf.Max(0, min);
            data[(int)FluxParticleInitialVelocityParameter.Max] = Mathf.Max(0, max);

            return (FluxParticleInitialVelocityParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleInitialVelocityParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleInitialVelocityParametersExtensions
    {
        /// <summary>
        /// Initial Speedの最小値を取得します。
        /// </summary>
        /// <param name="parameters">Initial Velocityパラメーター</param>
        /// <returns>Initial Speedの最小値</returns>
        [PublicAPI]
        public static float GetMin(this FluxParticleInitialVelocityParameters parameters)
        {
            return (float)parameters[(int)FluxParticleInitialVelocityParameter.Min].Double;
        }

        /// <summary>
        /// Initial Speedの最小値を設定します。
        /// </summary>
        /// <param name="parameters">Initial Velocityパラメーター</param>
        /// <param name="min">Initial Speedの最小値</param>
        [PublicAPI]
        public static void SetMin(this FluxParticleInitialVelocityParameters parameters, float min)
        {
            parameters[(int)FluxParticleInitialVelocityParameter.Min] = Mathf.Max(0, min);
        }

        /// <summary>
        /// Initial Speedの最大値を取得します。
        /// </summary>
        /// <param name="parameters">Initial Velocityパラメーター</param>
        /// <returns>Initial Speedの最大値</returns>
        [PublicAPI]
        public static float GetMax(this FluxParticleInitialVelocityParameters parameters)
        {
            return (float)parameters[(int)FluxParticleInitialVelocityParameter.Max].Double;
        }

        /// <summary>
        /// Initial Speedの最大値を設定します。
        /// </summary>
        /// <param name="parameters">Initial Velocityパラメーター</param>
        /// <param name="max">Initial Speedの最大値</param>
        [PublicAPI]
        public static void SetMax(this FluxParticleInitialVelocityParameters parameters, float max)
        {
            parameters[(int)FluxParticleInitialVelocityParameter.Max] = Mathf.Max(0, max);
        }
    }
}