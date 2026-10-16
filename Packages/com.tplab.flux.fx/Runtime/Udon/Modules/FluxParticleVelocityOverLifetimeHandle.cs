using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// VelocityOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleVelocityOverLifetimeHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// VelocityOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleVelocityOverLifetimeHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleVelocityOverLifetimeHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.VelocityOverLifetime);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleVelocityOverLifetimeHandle>(modules, FluxParticleModuleIds.VelocityOverLifetime);
#endif
        }
    }

    public static class FluxParticleVelocityOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 開始速度を取得します。
        /// </summary>
        public static Vector3 GetStart(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleVelocityOverLifetimeParameter.Start);

        /// <summary>
        /// 開始速度を変更します。
        /// </summary>
        public static bool SetStart(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVelocityOverLifetimeParameter.Start, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 終端速度を取得します。
        /// </summary>
        public static Vector3 GetEnd(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleVelocityOverLifetimeParameter.End);

        /// <summary>
        /// 終端速度を変更します。
        /// </summary>
        public static bool SetEnd(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVelocityOverLifetimeParameter.End, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 周回速度を取得します。
        /// </summary>
        public static Vector3 GetOrbital(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleVelocityOverLifetimeParameter.Orbital);

        /// <summary>
        /// 周回速度を変更します。
        /// </summary>
        public static bool SetOrbital(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVelocityOverLifetimeParameter.Orbital, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 中心のオフセットを取得します。
        /// </summary>
        public static Vector3 GetOffset(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleVelocityOverLifetimeParameter.Offset);

        /// <summary>
        /// 中心のオフセットを変更します。
        /// </summary>
        public static bool SetOffset(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVelocityOverLifetimeParameter.Offset, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 放射方向の速度を取得します。
        /// </summary>
        public static float GetRadial(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleVelocityOverLifetimeParameter.Radial).x;

        /// <summary>
        /// 放射方向の速度を変更します。
        /// </summary>
        public static bool SetRadial(this FluxParticleVelocityOverLifetimeHandle handle, float value) => handle.SetValue((int)FluxParticleVelocityOverLifetimeParameter.Radial, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

