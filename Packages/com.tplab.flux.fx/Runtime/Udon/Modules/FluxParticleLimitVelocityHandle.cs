using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

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
        internal static FluxParticleLimitVelocityHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleLimitVelocityHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.LimitVelocity);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleLimitVelocityHandle>(modules, FluxParticleModuleIds.LimitVelocity);
#endif
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

