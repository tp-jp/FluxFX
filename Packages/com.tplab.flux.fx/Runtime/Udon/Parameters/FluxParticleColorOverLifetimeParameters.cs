using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleColorOverLifetimeParameter
    {
        Enabled,
        EndColor,

        Count
    }

    /// <summary>
    /// ParticleのColor over Lifetimeに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleColorOverLifetimeParameters : DataList
    {
        internal static FluxParticleColorOverLifetimeParameters New(bool enabled, Color endColor)
        {
            var data = new DataToken[(int)FluxParticleColorOverLifetimeParameter.Count];

            data[(int)FluxParticleColorOverLifetimeParameter.Enabled] = enabled;
            data[(int)FluxParticleColorOverLifetimeParameter.EndColor] = new DataToken(endColor);

            return (FluxParticleColorOverLifetimeParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleColorOverLifetimeParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleColorOverLifetimeParametersExtensions
    {
        /// <summary>
        /// Color over Lifetimeが有効かどうかを取得します。
        /// </summary>
        /// <param name="parameters">Color over Lifetimeパラメーター</param>
        /// <returns>Color over Lifetimeが有効な場合はtrue</returns>
        [PublicAPI]
        public static bool GetEnabled(this FluxParticleColorOverLifetimeParameters parameters)
        {
            return parameters[(int)FluxParticleColorOverLifetimeParameter.Enabled].Boolean;
        }

        /// <summary>
        /// Color over Lifetimeの有効状態を設定します。
        /// </summary>
        /// <param name="parameters">Color over Lifetimeパラメーター</param>
        /// <param name="enabled">有効にする場合はtrue</param>
        [PublicAPI]
        public static void SetEnabled(this FluxParticleColorOverLifetimeParameters parameters, bool enabled)
        {
            parameters[(int)FluxParticleColorOverLifetimeParameter.Enabled] = enabled;
        }

        /// <summary>
        /// Linearモードの終了色を取得します。
        /// </summary>
        /// <param name="parameters">Color over Lifetimeパラメーター</param>
        /// <returns>Linearモードの終了色</returns>
        [PublicAPI]
        public static Color GetEndColor(this FluxParticleColorOverLifetimeParameters parameters)
        {
            return (Color)parameters[(int)FluxParticleColorOverLifetimeParameter.EndColor].Reference;
        }

        /// <summary>
        /// Linearモードの終了色を設定します。
        /// </summary>
        /// <param name="parameters">Color over Lifetimeパラメーター</param>
        /// <param name="endColor">Linearモードの終了色</param>
        [PublicAPI]
        public static void SetEndColor(this FluxParticleColorOverLifetimeParameters parameters, Color endColor)
        {
            parameters[(int)FluxParticleColorOverLifetimeParameter.EndColor] = new DataToken(endColor);
        }
    }
}