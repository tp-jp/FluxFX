using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// ParticleのForceに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleForceParameters : FluxParticleModuleParameters
    {
        internal static FluxParticleForceParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (FluxParticleForceParameters)data;
        }
    }

    /// <summary>
    /// FluxParticleForceParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleForceParametersExtensions
    {
        /// <summary>
        /// Particleに適用するForceを取得します。
        /// </summary>
        [PublicAPI]
        public static Vector3 GetForce(this FluxParticleForceParameters parameters)
        {
            var force = parameters.GetVector("_Force");

            return new Vector3(force.x, force.y, force.z);
        }

        /// <summary>
        /// Particleに適用するForceを設定します。
        /// </summary>
        [PublicAPI]
        public static void SetForce(this FluxParticleForceParameters parameters, Vector3 force)
        {
            parameters.SetVector("_Force", new Vector4(force.x, force.y, force.z, 0));
        }
    }
}