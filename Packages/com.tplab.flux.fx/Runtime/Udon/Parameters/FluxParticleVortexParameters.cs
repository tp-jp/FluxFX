using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;
using Mathf = UnityEngine.Mathf;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleVortexParameter
    {
        Center,
        Axis,
        Strength,

        Count
    }

    /// <summary>
    /// ParticleのVortexに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleVortexParameters : DataList
    {
        internal static FluxParticleVortexParameters New(Vector3 center, Vector3 axis, float strength)
        {
            var data = new DataToken[(int)FluxParticleVortexParameter.Count];

            data[(int)FluxParticleVortexParameter.Center] = new DataToken(center);
            data[(int)FluxParticleVortexParameter.Axis] = new DataToken(axis);
            data[(int)FluxParticleVortexParameter.Strength] = Mathf.Max(0, strength);

            return (FluxParticleVortexParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleVortexParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleVortexParametersExt
    {
        /// <summary>
        /// Vortexの中心位置を取得します。
        /// </summary>
        /// <param name="parameters">Vortexパラメーター</param>
        /// <returns>Vortexの中心位置</returns>
        [PublicAPI]
        public static Vector3 GetCenter(this FluxParticleVortexParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleVortexParameter.Center].Reference;
        }

        /// <summary>
        /// Vortexの中心位置を設定します。
        /// </summary>
        /// <param name="parameters">Vortexパラメーター</param>
        /// <param name="center">Vortexの中心位置</param>
        [PublicAPI]
        public static void SetCenter(this FluxParticleVortexParameters parameters, Vector3 center)
        {
            parameters[(int)FluxParticleVortexParameter.Center] = new DataToken(center);
        }

        /// <summary>
        /// Vortexの軸を取得します。
        /// </summary>
        /// <param name="parameters">Vortexパラメーター</param>
        /// <returns>Vortexの軸</returns>
        [PublicAPI]
        public static Vector3 GetAxis(this FluxParticleVortexParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleVortexParameter.Axis].Reference;
        }

        /// <summary>
        /// Vortexの軸を設定します。
        /// </summary>
        /// <param name="parameters">Vortexパラメーター</param>
        /// <param name="axis">Vortexの軸</param>
        [PublicAPI]
        public static void SetAxis(this FluxParticleVortexParameters parameters, Vector3 axis)
        {
            parameters[(int)FluxParticleVortexParameter.Axis] = new DataToken(axis);
        }

        /// <summary>
        /// Vortexの強度を取得します。
        /// </summary>
        /// <param name="parameters">Vortexパラメーター</param>
        /// <returns>Vortexの強度</returns>
        [PublicAPI]
        public static float GetStrength(this FluxParticleVortexParameters parameters)
        {
            return (float)parameters[(int)FluxParticleVortexParameter.Strength].Double;
        }

        /// <summary>
        /// Vortexの強度を設定します。
        /// </summary>
        /// <param name="parameters">Vortexパラメーター</param>
        /// <param name="strength">Vortexの強度</param>
        [PublicAPI]
        public static void SetStrength(this FluxParticleVortexParameters parameters, float strength)
        {
            parameters[(int)FluxParticleVortexParameter.Strength] = Mathf.Max(0, strength);
        }
    }
}