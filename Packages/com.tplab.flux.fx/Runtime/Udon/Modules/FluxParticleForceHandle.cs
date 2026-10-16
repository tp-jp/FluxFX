using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Forceの操作ハンドル
    /// </summary>
    public class FluxParticleForceHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Forceのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleForceHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.force";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleForceHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleForceHandle)new DataList();
#else
            var handle = new FluxParticleForceHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleForceHandleExtensions
    {
        /// <summary>
        /// 加速度を取得します。
        /// </summary>
        public static Vector3 GetForce(this FluxParticleForceHandle handle) => handle.GetValue((int)FluxParticleForceParameter.Force);

        /// <summary>
        /// 加速度を変更します。
        /// </summary>
        public static bool SetForce(this FluxParticleForceHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleForceParameter.Force, FluxParticleValueType.Float3, value);
    }
}

