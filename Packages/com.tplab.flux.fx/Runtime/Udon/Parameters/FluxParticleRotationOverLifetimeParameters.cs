using JetBrains.Annotations;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleRotationOverLifetimeParameter
    {
        Enabled,
        EndRotation,

        Count
    }

    /// <summary>
    /// ParticleのRotation over Lifetimeに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleRotationOverLifetimeParameters : DataList
    {
        internal static FluxParticleRotationOverLifetimeParameters New(bool enabled, float endRotation)
        {
            var data = new DataToken[(int)FluxParticleRotationOverLifetimeParameter.Count];

            data[(int)FluxParticleRotationOverLifetimeParameter.Enabled] = enabled;
            data[(int)FluxParticleRotationOverLifetimeParameter.EndRotation] = endRotation;

            return (FluxParticleRotationOverLifetimeParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleRotationOverLifetimeParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleRotationOverLifetimeParametersExtensions
    {
        /// <summary>
        /// Rotation over Lifetimeが有効かどうかを取得します。
        /// </summary>
        /// <param name="parameters">Rotation over Lifetimeパラメーター</param>
        /// <returns>Rotation over Lifetimeが有効な場合はtrue</returns>
        [PublicAPI]
        public static bool GetEnabled(this FluxParticleRotationOverLifetimeParameters parameters)
        {
            return parameters[(int)FluxParticleRotationOverLifetimeParameter.Enabled].Boolean;
        }

        /// <summary>
        /// Rotation over Lifetimeの有効状態を設定します。
        /// </summary>
        /// <param name="parameters">Rotation over Lifetimeパラメーター</param>
        /// <param name="enabled">有効にする場合はtrue</param>
        [PublicAPI]
        public static void SetEnabled(this FluxParticleRotationOverLifetimeParameters parameters, bool enabled)
        {
            parameters[(int)FluxParticleRotationOverLifetimeParameter.Enabled] = enabled;
        }

        /// <summary>
        /// Linearモードの終了回転角を取得します。
        /// </summary>
        /// <param name="parameters">Rotation over Lifetimeパラメーター</param>
        /// <returns>Linearモードの終了回転角</returns>
        [PublicAPI]
        public static float GetEndRotation(this FluxParticleRotationOverLifetimeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleRotationOverLifetimeParameter.EndRotation].Double;
        }

        /// <summary>
        /// Linearモードの終了回転角を設定します。
        /// </summary>
        /// <param name="parameters">Rotation over Lifetimeパラメーター</param>
        /// <param name="endRotation">Linearモードの終了回転角</param>
        [PublicAPI]
        public static void SetEndRotation(this FluxParticleRotationOverLifetimeParameters parameters, float endRotation)
        {
            parameters[(int)FluxParticleRotationOverLifetimeParameter.EndRotation] = endRotation;
        }
    }
}