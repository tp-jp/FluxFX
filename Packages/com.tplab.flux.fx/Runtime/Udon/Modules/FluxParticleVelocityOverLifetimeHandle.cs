using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// VelocityOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleVelocityOverLifetimeHandle : FluxParticleModuleHandle
    {
        internal const int StartParameterIndex = 0;
        internal const int EndParameterIndex = 1;
        internal const int OrbitalParameterIndex = 2;
        internal const int OffsetParameterIndex = 3;
        internal const int RadialParameterIndex = 4;

        /// <summary>
        /// VelocityOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleVelocityOverLifetimeHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.velocity-over-lifetime";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleVelocityOverLifetimeHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleVelocityOverLifetimeHandle)new DataList();
#else
            var handle = new FluxParticleVelocityOverLifetimeHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleVelocityOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 開始速度を取得します。
        /// </summary>
        public static Vector3 GetStart(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue(FluxParticleVelocityOverLifetimeHandle.StartParameterIndex);

        /// <summary>
        /// 開始速度を変更します。
        /// </summary>
        public static bool SetStart(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue(FluxParticleVelocityOverLifetimeHandle.StartParameterIndex, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 終端速度を取得します。
        /// </summary>
        public static Vector3 GetEnd(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue(FluxParticleVelocityOverLifetimeHandle.EndParameterIndex);

        /// <summary>
        /// 終端速度を変更します。
        /// </summary>
        public static bool SetEnd(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue(FluxParticleVelocityOverLifetimeHandle.EndParameterIndex, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 周回速度を取得します。
        /// </summary>
        public static Vector3 GetOrbital(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue(FluxParticleVelocityOverLifetimeHandle.OrbitalParameterIndex);

        /// <summary>
        /// 周回速度を変更します。
        /// </summary>
        public static bool SetOrbital(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue(FluxParticleVelocityOverLifetimeHandle.OrbitalParameterIndex, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 中心のオフセットを取得します。
        /// </summary>
        public static Vector3 GetOffset(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue(FluxParticleVelocityOverLifetimeHandle.OffsetParameterIndex);

        /// <summary>
        /// 中心のオフセットを変更します。
        /// </summary>
        public static bool SetOffset(this FluxParticleVelocityOverLifetimeHandle handle, Vector3 value) => handle.SetValue(FluxParticleVelocityOverLifetimeHandle.OffsetParameterIndex, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 放射方向の速度を取得します。
        /// </summary>
        public static float GetRadial(this FluxParticleVelocityOverLifetimeHandle handle) => handle.GetValue(FluxParticleVelocityOverLifetimeHandle.RadialParameterIndex).x;

        /// <summary>
        /// 放射方向の速度を変更します。
        /// </summary>
        public static bool SetRadial(this FluxParticleVelocityOverLifetimeHandle handle, float value) => handle.SetValue(FluxParticleVelocityOverLifetimeHandle.RadialParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

