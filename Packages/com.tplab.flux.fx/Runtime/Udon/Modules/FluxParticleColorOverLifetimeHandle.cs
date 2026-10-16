using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// ColorOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleColorOverLifetimeHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// ColorOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        internal static FluxParticleColorOverLifetimeHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleColorOverLifetimeHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.ColorOverLifetime);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleColorOverLifetimeHandle>(modules, FluxParticleModuleIds.ColorOverLifetime);
#endif
        }
    }

    public static class FluxParticleColorOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 現在のモードが終端色の操作に対応しているかを返します。
        /// </summary>
        public static bool HasEndColor(this FluxParticleColorOverLifetimeHandle handle) => handle.HasParameter((int)FluxParticleColorOverLifetimeParameter.EndColor);

        /// <summary>
        /// 終端色を取得します。非対応モードでは既定値を返します。
        /// </summary>
        public static Color GetEndColor(this FluxParticleColorOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleColorOverLifetimeParameter.EndColor);

        /// <summary>
        /// 終端色を変更します。非対応モードではfalseを返します。
        /// </summary>
        public static bool SetEndColor(this FluxParticleColorOverLifetimeHandle handle, Color value) => handle.SetValue((int)FluxParticleColorOverLifetimeParameter.EndColor, FluxParticleValueType.Float4, value);
    }
}

