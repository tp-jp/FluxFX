using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleLimitVelocityParameter
    {
        MaxSpeed,

        Count
    }

    /// <summary>
    /// ParticleのLimit Velocityに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleLimitVelocityParameters : DataList
    {
        internal static FluxParticleLimitVelocityParameters New(float maxSpeed)
        {
            var data = new DataToken[(int)FluxParticleLimitVelocityParameter.Count];

            data[(int)FluxParticleLimitVelocityParameter.MaxSpeed] = Mathf.Max(0, maxSpeed);

            return (FluxParticleLimitVelocityParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleLimitVelocityParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleLimitVelocityParametersExtensions
    {
        /// <summary>
        /// Particleの最大速度を取得します。
        /// </summary>
        /// <param name="parameters">Limit Velocityパラメーター</param>
        /// <returns>Particleの最大速度</returns>
        [PublicAPI]
        public static float GetMaxSpeed(this FluxParticleLimitVelocityParameters parameters)
        {
            return (float)parameters[(int)FluxParticleLimitVelocityParameter.MaxSpeed].Double;
        }

        /// <summary>
        /// Particleの最大速度を設定します。
        /// </summary>
        /// <param name="parameters">Limit Velocityパラメーター</param>
        /// <param name="maxSpeed">Particleの最大速度</param>
        [PublicAPI]
        public static void SetMaxSpeed(this FluxParticleLimitVelocityParameters parameters, float maxSpeed)
        {
            parameters[(int)FluxParticleLimitVelocityParameter.MaxSpeed] = Mathf.Max(0, maxSpeed);
        }
    }
}