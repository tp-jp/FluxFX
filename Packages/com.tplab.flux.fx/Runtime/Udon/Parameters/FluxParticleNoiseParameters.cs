using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleNoiseParameter
    {
        Store,
        EnabledParameter,

        Count
    }

    /// <summary>
    /// ParticleのNoiseに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleNoiseParameters : DataList
    {
        internal static FluxParticleNoiseParameters New(DataList binding)
        {
            var data = new DataToken[(int)FluxParticleNoiseParameter.Count];

            data[(int)FluxParticleNoiseParameter.Store] = binding[0];
            data[(int)FluxParticleNoiseParameter.EnabledParameter] = binding[1];

            return (FluxParticleNoiseParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleNoiseParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleNoiseParametersExtensions
    {
        /// <summary>
        /// Noiseの強度を取得します。
        /// </summary>
        [PublicAPI]
        public static float GetStrength(this FluxParticleNoiseParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleNoiseParameter.Store].Reference;

            return store.GetFloat("_NoiseStrength");
        }

        /// <summary>
        /// Noiseの強度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetStrength(this FluxParticleNoiseParameters parameters, float strength)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleNoiseParameter.Store].Reference;

            store.SetFloat("_NoiseStrength", Mathf.Max(0, strength));
        }

        /// <summary>
        /// Noiseのスケールを取得します。
        /// </summary>
        [PublicAPI]
        public static float GetScale(this FluxParticleNoiseParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleNoiseParameter.Store].Reference;

            return store.GetFloat("_NoiseScale");
        }

        /// <summary>
        /// Noiseのスケールを設定します。
        /// </summary>
        [PublicAPI]
        public static void SetScale(this FluxParticleNoiseParameters parameters, float scale)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleNoiseParameter.Store].Reference;

            store.SetFloat("_NoiseScale", Mathf.Max(0, scale));
        }

        /// <summary>
        /// Noiseの時間変化速度を取得します。
        /// </summary>
        [PublicAPI]
        public static float GetSpeed(this FluxParticleNoiseParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleNoiseParameter.Store].Reference;

            return store.GetFloat("_NoiseSpeed");
        }

        /// <summary>
        /// Noiseの時間変化速度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetSpeed(this FluxParticleNoiseParameters parameters, float speed)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleNoiseParameter.Store].Reference;

            store.SetFloat("_NoiseSpeed", Mathf.Max(0, speed));
        }
    }
}