using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleGravityParameter
    {
        Gravity,

        Count
    }

    /// <summary>
    /// ParticleのGravityに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleGravityParameters : DataList
    {
        internal static FluxParticleGravityParameters New(Vector3 gravity)
        {
            var data = new DataToken[(int)FluxParticleGravityParameter.Count];

            data[(int)FluxParticleGravityParameter.Gravity] = new DataToken(gravity);

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
        /// <param name="parameters">Gravityパラメーター</param>
        /// <returns>Particleに適用するGravity</returns>
        [PublicAPI]
        public static Vector3 GetGravity(this FluxParticleGravityParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleGravityParameter.Gravity].Reference;
        }

        /// <summary>
        /// Particleに適用するGravityを設定します。
        /// </summary>
        /// <param name="parameters">Gravityパラメーター</param>
        /// <param name="gravity">Particleに適用するGravity</param>
        [PublicAPI]
        public static void SetGravity(this FluxParticleGravityParameters parameters, Vector3 gravity)
        {
            parameters[(int)FluxParticleGravityParameter.Gravity] = new DataToken(gravity);
        }
    }
}