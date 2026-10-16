using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// SizeOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleSizeOverLifetimeHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// SizeOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        internal static FluxParticleSizeOverLifetimeHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleSizeOverLifetimeHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.SizeOverLifetime);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleSizeOverLifetimeHandle>(modules, FluxParticleModuleIds.SizeOverLifetime);
#endif
        }
    }

    public static class FluxParticleSizeOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 現在のモードが終端サイズの操作に対応しているかを返します。
        /// </summary>
        public static bool HasEndSize(this FluxParticleSizeOverLifetimeHandle handle) => handle.HasParameter((int)FluxParticleSizeOverLifetimeParameter.EndSize);

        /// <summary>
        /// 終端サイズを取得します。非対応モードでは既定値を返します。
        /// </summary>
        public static float GetEndSize(this FluxParticleSizeOverLifetimeHandle handle) => handle.GetValue((int)FluxParticleSizeOverLifetimeParameter.EndSize).x;

        /// <summary>
        /// 終端サイズを変更します。非対応モードではfalseを返します。
        /// </summary>
        public static bool SetEndSize(this FluxParticleSizeOverLifetimeHandle handle, float value) => handle.SetValue((int)FluxParticleSizeOverLifetimeParameter.EndSize, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

