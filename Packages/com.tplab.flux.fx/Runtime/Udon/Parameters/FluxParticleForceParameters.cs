using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleForceParameter
    {
        Force,

        Count
    }

    /// <summary>
    /// ParticleのForceに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleForceParameters : DataList
    {
        internal static FluxParticleForceParameters New(Vector3 force)
        {
            var data = new DataToken[(int)FluxParticleForceParameter.Count];

            data[(int)FluxParticleForceParameter.Force] = new DataToken(force);

            return (FluxParticleForceParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleForceParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleForceParametersExt
    {
        /// <summary>
        /// Particleに適用するForceを取得します。
        /// </summary>
        /// <param name="parameters">Forceパラメーター</param>
        /// <returns>Particleに適用するForce</returns>
        [PublicAPI]
        public static Vector3 GetForce(this FluxParticleForceParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleForceParameter.Force].Reference;
        }

        /// <summary>
        /// Particleに適用するForceを設定します。
        /// </summary>
        /// <param name="parameters">Forceパラメーター</param>
        /// <param name="force">Particleに適用するForce</param>
        [PublicAPI]
        public static void SetForce(this FluxParticleForceParameters parameters, Vector3 force)
        {
            parameters[(int)FluxParticleForceParameter.Force] = new DataToken(force);
        }
    }
}