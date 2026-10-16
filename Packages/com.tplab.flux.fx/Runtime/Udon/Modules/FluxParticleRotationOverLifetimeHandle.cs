using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// RotationOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleRotationOverLifetimeHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// RotationOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        internal static FluxParticleRotationOverLifetimeHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleRotationOverLifetimeHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.RotationOverLifetime);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleRotationOverLifetimeHandle>(modules, FluxParticleModuleIds.RotationOverLifetime);
#endif
        }
    }

    public static class FluxParticleRotationOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 終端回転を取得します。
        /// </summary>
        public static Vector3 GetEndRotation(this FluxParticleRotationOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleRotationOverLifetimeParameter.EndRotation);

        /// <summary>
        /// 終端回転を変更します。
        /// </summary>
        public static bool SetEndRotation(this FluxParticleRotationOverLifetimeHandle handle, Vector3 value) => handle.SetValue((int)FluxParticleRotationOverLifetimeParameter.EndRotation, FluxParticleValueType.Float3, value);
    }
}

