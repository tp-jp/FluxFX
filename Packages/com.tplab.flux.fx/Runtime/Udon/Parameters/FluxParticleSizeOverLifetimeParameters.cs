using JetBrains.Annotations;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleSizeOverLifetimeParameter
    {
        Enabled,
        EndSize,

        Count
    }

    /// <summary>
    /// ParticleのSize over Lifetimeに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleSizeOverLifetimeParameters : DataList
    {
        internal static FluxParticleSizeOverLifetimeParameters New(bool enabled, float endSize)
        {
            var data = new DataToken[(int)FluxParticleSizeOverLifetimeParameter.Count];

            data[(int)FluxParticleSizeOverLifetimeParameter.Enabled] = enabled;
            data[(int)FluxParticleSizeOverLifetimeParameter.EndSize] = Mathf.Max(0, endSize);

            return (FluxParticleSizeOverLifetimeParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleSizeOverLifetimeParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleSizeOverLifetimeParametersExtensions
    {
        /// <summary>
        /// Size over Lifetimeが有効かどうかを取得します。
        /// </summary>
        /// <param name="parameters">Size over Lifetimeパラメーター</param>
        /// <returns>Size over Lifetimeが有効な場合はtrue</returns>
        [PublicAPI]
        public static bool GetEnabled(this FluxParticleSizeOverLifetimeParameters parameters)
        {
            return parameters[(int)FluxParticleSizeOverLifetimeParameter.Enabled].Boolean;
        }

        /// <summary>
        /// Size over Lifetimeの有効状態を設定します。
        /// </summary>
        /// <param name="parameters">Size over Lifetimeパラメーター</param>
        /// <param name="enabled">有効にする場合はtrue</param>
        [PublicAPI]
        public static void SetEnabled(this FluxParticleSizeOverLifetimeParameters parameters, bool enabled)
        {
            parameters[(int)FluxParticleSizeOverLifetimeParameter.Enabled] = enabled;
        }

        /// <summary>
        /// Linearモードの終了サイズを取得します。
        /// </summary>
        /// <param name="parameters">Size over Lifetimeパラメーター</param>
        /// <returns>Linearモードの終了サイズ</returns>
        [PublicAPI]
        public static float GetEndSize(this FluxParticleSizeOverLifetimeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleSizeOverLifetimeParameter.EndSize].Double;
        }

        /// <summary>
        /// Linearモードの終了サイズを設定します。
        /// </summary>
        /// <param name="parameters">Size over Lifetimeパラメーター</param>
        /// <param name="endSize">Linearモードの終了サイズ</param>
        [PublicAPI]
        public static void SetEndSize(this FluxParticleSizeOverLifetimeParameters parameters, float endSize)
        {
            parameters[(int)FluxParticleSizeOverLifetimeParameter.EndSize] = Mathf.Max(0, endSize);
        }
    }
}