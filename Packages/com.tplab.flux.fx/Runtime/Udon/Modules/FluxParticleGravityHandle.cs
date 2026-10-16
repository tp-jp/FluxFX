using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Gravityの操作ハンドル
    /// </summary>
    public class FluxParticleGravityHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Gravityのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleGravityHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleGravityHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.Gravity);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleGravityHandle>(modules, FluxParticleModuleIds.Gravity);
#endif
        }
    }

    public static class FluxParticleGravityHandleExtensions
    {
        /// <summary>
        /// 重力加速度を取得します。
        /// </summary>
        public static Vector3 GetGravity(this FluxParticleGravityHandle handle) => handle.GetValue((int)FluxParticleGravityParameter.Gravity);

        /// <summary>
        /// 重力加速度を変更します。
        /// </summary>
        public static bool SetGravity(this FluxParticleGravityHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleGravityParameter.Gravity, FluxParticleValueType.Float3, value);
    }
}
