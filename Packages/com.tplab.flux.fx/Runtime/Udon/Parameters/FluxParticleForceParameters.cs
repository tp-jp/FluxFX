using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleForceParameter
    {
        Store,
        EnabledParameter,

        Count
    }

    /// <summary>
    /// ParticleのForceに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleForceParameters : DataList
    {
        internal static FluxParticleForceParameters New(FluxParticleGpuParameterStore store, string enabledParameter)
        {
            var data = new DataToken[(int)FluxParticleForceParameter.Count];

            data[(int)FluxParticleForceParameter.Store] = new DataToken(store);
            data[(int)FluxParticleForceParameter.EnabledParameter] = enabledParameter;

            return (FluxParticleForceParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleForceParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleForceParametersExtensions
    {
        /// <summary>
        /// Particleに適用するForceを取得します。
        /// </summary>
        [PublicAPI]
        public static Vector3 GetForce(this FluxParticleForceParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleForceParameter.Store].Reference;
            var force = store.GetVector("_Force");

            return new Vector3(force.x, force.y, force.z);
        }

        /// <summary>
        /// Particleに適用するForceを設定します。
        /// </summary>
        [PublicAPI]
        public static void SetForce(this FluxParticleForceParameters parameters, Vector3 force)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleForceParameter.Store].Reference;

            store.SetVector("_Force", new Vector4(force.x, force.y, force.z, 0));
        }
    }
}