using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    enum FluxParticleVelocityOverLifetimeParameter
    {
        Enabled,
        Start,
        End,
        Offset,
        Radial,

        Count
    }

    /// <summary>
    /// ParticleのVelocity over Lifetimeに関するRuntimeパラメーターを保持します。
    /// </summary>
    [PublicAPI]
    public class FluxParticleVelocityOverLifetimeParameters : DataList
    {
        internal static FluxParticleVelocityOverLifetimeParameters New(bool enabled, Vector3 start, Vector3 end)
        {
            var data = new DataToken[(int)FluxParticleVelocityOverLifetimeParameter.Count];

            data[(int)FluxParticleVelocityOverLifetimeParameter.Enabled] = enabled;
            data[(int)FluxParticleVelocityOverLifetimeParameter.Start] = new DataToken(start);
            data[(int)FluxParticleVelocityOverLifetimeParameter.End] = new DataToken(end);
            data[(int)FluxParticleVelocityOverLifetimeParameter.Offset] = new DataToken(Vector3.zero);
            data[(int)FluxParticleVelocityOverLifetimeParameter.Radial] = 0.0f;

            return (FluxParticleVelocityOverLifetimeParameters)new DataList(data);
        }
    }

    /// <summary>
    /// FluxParticleVelocityOverLifetimeParametersを操作するための拡張メソッドを提供します。
    /// </summary>
    [PublicAPI]
    public static class FluxParticleVelocityOverLifetimeParametersExtensions
    {
        /// <summary>
        /// Velocity over Lifetimeが有効かどうかを取得します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <returns>Velocity over Lifetimeが有効な場合はtrue</returns>
        [PublicAPI]
        public static bool GetEnabled(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            return parameters[(int)FluxParticleVelocityOverLifetimeParameter.Enabled].Boolean;
        }

        /// <summary>
        /// Velocity over Lifetimeの有効状態を設定します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <param name="enabled">有効にする場合はtrue</param>
        [PublicAPI]
        public static void SetEnabled(this FluxParticleVelocityOverLifetimeParameters parameters, bool enabled)
        {
            parameters[(int)FluxParticleVelocityOverLifetimeParameter.Enabled] = enabled;
        }

        /// <summary>
        /// Lifetime開始時のVelocityを取得します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <returns>Lifetime開始時のVelocity</returns>
        [PublicAPI]
        public static Vector3 GetStart(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleVelocityOverLifetimeParameter.Start].Reference;
        }

        /// <summary>
        /// Lifetime開始時のVelocityを設定します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <param name="start">Lifetime開始時のVelocity</param>
        [PublicAPI]
        public static void SetStart(this FluxParticleVelocityOverLifetimeParameters parameters, Vector3 start)
        {
            parameters[(int)FluxParticleVelocityOverLifetimeParameter.Start] = new DataToken(start);
        }

        /// <summary>
        /// Lifetime終了時のVelocityを取得します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <returns>Lifetime終了時のVelocity</returns>
        [PublicAPI]
        public static Vector3 GetEnd(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleVelocityOverLifetimeParameter.End].Reference;
        }

        /// <summary>
        /// Lifetime終了時のVelocityを設定します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <param name="end">Lifetime終了時のVelocity</param>
        [PublicAPI]
        public static void SetEnd(this FluxParticleVelocityOverLifetimeParameters parameters, Vector3 end)
        {
            parameters[(int)FluxParticleVelocityOverLifetimeParameter.End] = new DataToken(end);
        }

        /// <summary>
        /// Radial Velocityの中心Offsetを取得します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <returns>Radial Velocityの中心Offset</returns>
        [PublicAPI]
        public static Vector3 GetOffset(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            return (Vector3)parameters[(int)FluxParticleVelocityOverLifetimeParameter.Offset].Reference;
        }

        /// <summary>
        /// Radial Velocityの中心Offsetを設定します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <param name="offset">Radial Velocityの中心Offset</param>
        [PublicAPI]
        public static void SetOffset(this FluxParticleVelocityOverLifetimeParameters parameters, Vector3 offset)
        {
            parameters[(int)FluxParticleVelocityOverLifetimeParameter.Offset] = new DataToken(offset);
        }

        /// <summary>
        /// 中心から放射方向へ加算するVelocityを取得します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <returns>放射方向へ加算するVelocity</returns>
        [PublicAPI]
        public static float GetRadial(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            return (float)parameters[(int)FluxParticleVelocityOverLifetimeParameter.Radial].Double;
        }

        /// <summary>
        /// 中心から放射方向へ加算するVelocityを設定します。
        /// </summary>
        /// <param name="parameters">Velocity over Lifetimeパラメーター</param>
        /// <param name="radial">放射方向へ加算するVelocity</param>
        [PublicAPI]
        public static void SetRadial(this FluxParticleVelocityOverLifetimeParameters parameters, float radial)
        {
            parameters[(int)FluxParticleVelocityOverLifetimeParameter.Radial] = radial;
        }
    }
}