using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// ParticleのDragに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleDragParameters : FluxParticleModuleParameters
    {
        internal static FluxParticleDragParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (FluxParticleDragParameters)data;
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
            return parameters.GetFloat("_Drag");
        }

        /// <summary>
        /// Particleに適用するDragを設定します。
        /// </summary>
        [PublicAPI]
        public static void SetDrag(this FluxParticleDragParameters parameters, float drag)
        {
            parameters.SetFloat("_Drag", Mathf.Max(0, drag));
        }
    }
}