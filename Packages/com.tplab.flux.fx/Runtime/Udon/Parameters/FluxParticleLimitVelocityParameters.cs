using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleLimitVelocityParameter
    {
        Store,
        EnabledParameter,

        Count
    }

    /// <summary>
    /// ParticleのLimit Velocityに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleLimitVelocityParameters : DataList
    {
        internal static FluxParticleLimitVelocityParameters New(DataList binding)
        {
            var data = new DataToken[(int)FluxParticleLimitVelocityParameter.Count];

            data[(int)FluxParticleLimitVelocityParameter.Store] = binding[0];
            data[(int)FluxParticleLimitVelocityParameter.EnabledParameter] = binding[1];

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
        [PublicAPI]
        public static float GetMaxSpeed(this FluxParticleLimitVelocityParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleLimitVelocityParameter.Store].Reference;

            return store.GetFloat("_MaxSpeed");
        }

        /// <summary>
        /// Particleの最大速度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetMaxSpeed(this FluxParticleLimitVelocityParameters parameters, float maxSpeed)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleLimitVelocityParameter.Store].Reference;

            store.SetFloat("_MaxSpeed", Mathf.Max(0, maxSpeed));
        }
    }
}