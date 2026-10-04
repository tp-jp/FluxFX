using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleDragParameter
    {
        Drag,

        Count
    }

    /// <summary>
    /// ParticleのDragに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleDragParameters : DataList
    {
        internal static FluxParticleDragParameters New(float drag)
        {
            var data = new DataToken[(int)FluxParticleDragParameter.Count];

            data[(int)FluxParticleDragParameter.Drag] = Mathf.Max(0, drag);

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
        /// <param name="parameters">Dragパラメーター</param>
        /// <returns>Particleに適用するDrag</returns>
        [PublicAPI]
        public static float GetDrag(this FluxParticleDragParameters parameters)
        {
            return (float)parameters[(int)FluxParticleDragParameter.Drag].Double;
        }

        /// <summary>
        /// Particleに適用するDragを設定します。
        /// </summary>
        /// <param name="parameters">Dragパラメーター</param>
        /// <param name="drag">Particleに適用するDrag</param>
        [PublicAPI]
        public static void SetDrag(this FluxParticleDragParameters parameters, float drag)
        {
            parameters[(int)FluxParticleDragParameter.Drag] = Mathf.Max(0, drag);
        }
    }
}