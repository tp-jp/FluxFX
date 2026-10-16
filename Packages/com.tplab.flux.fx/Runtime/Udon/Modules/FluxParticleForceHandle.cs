using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

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
#if COMPILER_UDONSHARP
            return (FluxParticleForceHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.Force);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleForceHandle>(modules, FluxParticleModuleIds.Force);
#endif
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

