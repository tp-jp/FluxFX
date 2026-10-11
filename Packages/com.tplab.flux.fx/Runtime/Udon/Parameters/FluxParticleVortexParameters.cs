using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleVortexParameter
    {
        Store,
        EnabledParameter,

        Count
    }

    /// <summary>
    /// ParticleのVortexに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleVortexParameters : DataList
    {
        internal static FluxParticleVortexParameters New(FluxParticleGpuParameterStore store, string enabledParameter)
        {
            var data = new DataToken[(int)FluxParticleVortexParameter.Count];

            data[(int)FluxParticleVortexParameter.Store] = new DataToken(store);
            data[(int)FluxParticleVortexParameter.EnabledParameter] = enabledParameter;

            return (FluxParticleVortexParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleVortexParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleVortexParametersExtensions
    {
        /// <summary>
        /// Vortexの中心位置を取得します。
        /// </summary>
        [PublicAPI]
        public static Vector3 GetCenter(this FluxParticleVortexParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleVortexParameter.Store].Reference;
            var center = store.GetVector("_VortexCenter");

            return new Vector3(center.x, center.y, center.z);
        }

        /// <summary>
        /// Vortexの中心位置を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetCenter(this FluxParticleVortexParameters parameters, Vector3 center)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleVortexParameter.Store].Reference;

            store.SetVector("_VortexCenter", new Vector4(center.x, center.y, center.z, 0));
        }

        /// <summary>
        /// Vortexの軸を取得します。
        /// </summary>
        [PublicAPI]
        public static Vector3 GetAxis(this FluxParticleVortexParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleVortexParameter.Store].Reference;
            var axis = store.GetVector("_VortexAxis");

            return new Vector3(axis.x, axis.y, axis.z);
        }

        /// <summary>
        /// Vortexの軸を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetAxis(this FluxParticleVortexParameters parameters, Vector3 axis)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleVortexParameter.Store].Reference;
            var normalized = axis.normalized;

            store.SetVector("_VortexAxis", new Vector4(normalized.x, normalized.y, normalized.z, 0));
        }

        /// <summary>
        /// Vortexの強度を取得します。
        /// </summary>
        [PublicAPI]
        public static float GetStrength(this FluxParticleVortexParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleVortexParameter.Store].Reference;

            return store.GetFloat("_VortexStrength");
        }

        /// <summary>
        /// Vortexの強度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetStrength(this FluxParticleVortexParameters parameters, float strength)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleVortexParameter.Store].Reference;

            store.SetFloat("_VortexStrength", Mathf.Max(0, strength));
        }
    }
}