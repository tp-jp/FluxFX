using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// ParticleのLimit Velocityに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleLimitVelocityParameters : FluxParticleModuleParameters
    {
        internal static FluxParticleLimitVelocityParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (FluxParticleLimitVelocityParameters)data;
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
        [PublicAPI]
        public static float GetMaxSpeed(this FluxParticleLimitVelocityParameters parameters)
        {
            return parameters.GetFloat("_MaxSpeed");
        }

        /// <summary>
        /// Particleの最大速度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetMaxSpeed(this FluxParticleLimitVelocityParameters parameters, float maxSpeed)
        {
            parameters.SetFloat("_MaxSpeed", Mathf.Max(0, maxSpeed));
        }
    }
}