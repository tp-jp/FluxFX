using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Gravityの操作ハンドル
    /// </summary>
    public class FluxParticleGravityHandle : FluxParticleModuleHandle
    {
        internal const int GravityParameterIndex = 0;

        /// <summary>
        /// Gravityのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleGravityHandle Get(FluxParticleModules modules)
        {
            const string id = "fluxfx.gravity";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleGravityHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleGravityHandle)new DataList();
#else
            var handle = new FluxParticleGravityHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleGravityHandleExtensions
    {
        /// <summary>
        /// 重力加速度を取得します。
        /// </summary>
        public static Vector3 GetGravity(this FluxParticleGravityHandle handle) => handle.GetValue(FluxParticleGravityHandle.GravityParameterIndex);

        /// <summary>
        /// 重力加速度を変更します。
        /// </summary>
        public static bool SetGravity(this FluxParticleGravityHandle handle, Vector3 value) => handle.SetValue(FluxParticleGravityHandle.GravityParameterIndex, FluxParticleValueType.Float3, value);
    }
}
