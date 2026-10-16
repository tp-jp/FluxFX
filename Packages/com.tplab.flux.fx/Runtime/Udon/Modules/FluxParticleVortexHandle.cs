using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Vortexの操作ハンドル
    /// </summary>
    public class FluxParticleVortexHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Vortexのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleVortexHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.vortex";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleVortexHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleVortexHandle)new DataList();
#else
            var handle = new FluxParticleVortexHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleVortexHandleExtensions
    {
        /// <summary>
        /// 中心を取得します。
        /// </summary>
        public static Vector3 GetCenter(this FluxParticleVortexHandle handle) => handle.GetValue((int)FluxParticleVortexParameter.Center);

        /// <summary>
        /// 中心を変更します。
        /// </summary>
        public static bool SetCenter(this FluxParticleVortexHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVortexParameter.Center, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 回転軸を取得します。
        /// </summary>
        public static Vector3 GetAxis(this FluxParticleVortexHandle handle) => handle.GetValue((int)FluxParticleVortexParameter.Axis);

        /// <summary>
        /// 回転軸を変更します。
        /// </summary>
        public static bool SetAxis(this FluxParticleVortexHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleVortexParameter.Axis, FluxParticleValueType.Float3, value);

        /// <summary>
        /// 強さを取得します。
        /// </summary>
        public static float GetStrength(this FluxParticleVortexHandle handle) => handle.GetValue((int)FluxParticleVortexParameter.Strength).x;

        /// <summary>
        /// 強さを変更します。
        /// </summary>
        public static bool SetStrength(this FluxParticleVortexHandle handle, float value) => handle.SetValue((int)FluxParticleVortexParameter.Strength, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

