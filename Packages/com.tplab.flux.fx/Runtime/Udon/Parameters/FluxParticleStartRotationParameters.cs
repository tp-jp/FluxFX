using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleStartRotationParameter
    {
        Rotation,

        Count
    }

    /// <summary>
    /// ParticleのStart Rotationに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleStartRotationParameters : DataList
    {
        internal static FluxParticleStartRotationParameters New(Vector3 rotation)
        {
            var data = new DataToken[(int)FluxParticleStartRotationParameter.Count];

            data[(int)FluxParticleStartRotationParameter.Rotation] = new DataToken(rotation);

            return (FluxParticleStartRotationParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleStartRotationParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleStartRotationParametersExtensions
    {
        /// <summary>
        /// Start Rotationを取得します。
        /// </summary>
        /// <param name="parameters">Start Rotationパラメーター</param>
        /// <returns>Start Rotation</returns>
        [PublicAPI]
        public static Vector3 GetRotation(this FluxParticleStartRotationParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleStartRotationParameter.Rotation].Reference;
        }

        /// <summary>
        /// Start Rotationを設定します。
        /// </summary>
        /// <param name="parameters">Start Rotationパラメーター</param>
        /// <param name="rotation">Start Rotation</param>
        [PublicAPI]
        public static void SetRotation(this FluxParticleStartRotationParameters parameters, Vector3 rotation)
        {
            parameters[(int)FluxParticleStartRotationParameter.Rotation] = new DataToken(rotation);
        }
    }
}