using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleGravityParameter
    {
        Store,
        EnabledParameter,

        Count
    }

    /// <summary>
    /// ParticleのGravityに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleGravityParameters : DataList
    {
        internal static FluxParticleGravityParameters New(FluxParticleGpuParameterStore store, string enabledParameter)
        {
            var data = new DataToken[(int)FluxParticleGravityParameter.Count];

            data[(int)FluxParticleGravityParameter.Store] = new DataToken(store);
            data[(int)FluxParticleGravityParameter.EnabledParameter] = enabledParameter;

            return (FluxParticleGravityParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleGravityParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleGravityParametersExtensions
    {
        /// <summary>
        /// Particleに適用するGravityを取得します。
        /// </summary>
        [PublicAPI]
        public static Vector3 GetGravity(this FluxParticleGravityParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleGravityParameter.Store].Reference;
            var gravity = store.GetVector("_Gravity");

            return new Vector3(gravity.x, gravity.y, gravity.z);
        }

        /// <summary>
        /// Particleに適用するGravityを設定します。
        /// </summary>
        [PublicAPI]
        public static void SetGravity(this FluxParticleGravityParameters parameters, Vector3 gravity)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleGravityParameter.Store].Reference;

            store.SetVector("_Gravity", new Vector4(gravity.x, gravity.y, gravity.z, 0));
        }
    }
}