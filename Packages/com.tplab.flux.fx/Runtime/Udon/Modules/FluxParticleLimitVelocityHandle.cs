using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// LimitVelocityの操作ハンドル
    /// </summary>
    public class FluxParticleLimitVelocityHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// LimitVelocityのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleLimitVelocityHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.limit-velocity";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleLimitVelocityHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleLimitVelocityHandle)new DataList();
#else
            var handle = new FluxParticleLimitVelocityHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleLimitVelocityHandleExtensions
    {
        /// <summary>
        /// 速度の上限を取得します。
        /// </summary>
        public static float GetMaxSpeed(this FluxParticleLimitVelocityHandle handle) => handle.GetValue((int)FluxParticleLimitVelocityParameter.MaxSpeed).x;

        /// <summary>
        /// 速度の上限を変更します。
        /// </summary>
        public static bool SetMaxSpeed(this FluxParticleLimitVelocityHandle handle, float value) => handle.SetValue((int)FluxParticleLimitVelocityParameter.MaxSpeed, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

