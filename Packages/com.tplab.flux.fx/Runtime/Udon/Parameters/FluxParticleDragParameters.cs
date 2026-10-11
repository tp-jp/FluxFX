using JetBrains.Annotations;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleDragParameter
    {
        Store,

        Count
    }

    /// <summary>
    /// ParticleのDragに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleDragParameters : DataList
    {
        internal static FluxParticleDragParameters New(FluxParticleGpuParameterStore store)
        {
            var data = new DataToken[(int)FluxParticleDragParameter.Count];

            data[(int)FluxParticleDragParameter.Store] = new DataToken(store);

            return (FluxParticleDragParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleDragParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleDragParametersExtensions
    {
        /// <summary>
        /// Particleに適用するDragを取得します。
        /// </summary>
        [PublicAPI]
        public static float GetDrag(this FluxParticleDragParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleDragParameter.Store].Reference;

            return store.GetFloat("_Drag");
        }

        /// <summary>
        /// Particleに適用するDragを設定します。
        /// </summary>
        [PublicAPI]
        public static void SetDrag(this FluxParticleDragParameters parameters, float drag)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)FluxParticleDragParameter.Store].Reference;

            store.SetFloat("_Drag", Mathf.Max(0, drag));
        }
    }
}