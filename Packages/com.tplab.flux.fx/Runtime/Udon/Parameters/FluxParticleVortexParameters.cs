using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// ParticleのVortexに関するRuntimeパラメーターを操作します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleVortexParameters : FluxParticleModuleParameters
    {
        internal static FluxParticleVortexParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (FluxParticleVortexParameters)data;
        }
    }

    /// <summary>
    /// FluxParticleVortexParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleVortexParametersExtensions
    {
        /// <summary>
        /// Vortexの中心位置を取得します。
        /// </summary>
        [PublicAPI]
        public static Vector3 GetCenter(this FluxParticleVortexParameters parameters)
        {
            var center = parameters.GetVector("_VortexCenter");

            return new Vector3(center.x, center.y, center.z);
        }

        /// <summary>
        /// Vortexの中心位置を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetCenter(this FluxParticleVortexParameters parameters, Vector3 center)
        {
            parameters.SetVector("_VortexCenter", new Vector4(center.x, center.y, center.z, 0));
        }

        /// <summary>
        /// Vortexの軸を取得します。
        /// </summary>
        [PublicAPI]
        public static Vector3 GetAxis(this FluxParticleVortexParameters parameters)
        {
            var axis = parameters.GetVector("_VortexAxis");

            return new Vector3(axis.x, axis.y, axis.z);
        }

        /// <summary>
        /// Vortexの軸を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetAxis(this FluxParticleVortexParameters parameters, Vector3 axis)
        {
            var normalized = axis.normalized;

            parameters.SetVector("_VortexAxis", new Vector4(normalized.x, normalized.y, normalized.z, 0));
        }

        /// <summary>
        /// Vortexの強度を取得します。
        /// </summary>
        [PublicAPI]
        public static float GetStrength(this FluxParticleVortexParameters parameters)
        {
            return parameters.GetFloat("_VortexStrength");
        }

        /// <summary>
        /// Vortexの強度を設定します。
        /// </summary>
        [PublicAPI]
        public static void SetStrength(this FluxParticleVortexParameters parameters, float strength)
        {
            parameters.SetFloat("_VortexStrength", Mathf.Max(0, strength));
        }
    }
}