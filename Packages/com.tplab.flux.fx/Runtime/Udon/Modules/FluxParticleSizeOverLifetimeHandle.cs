using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// SizeOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleSizeOverLifetimeHandle : FluxParticleModuleHandle
    {
        internal const int EndSizeParameterIndex = 0;

        /// <summary>
        /// SizeOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleSizeOverLifetimeHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.size-over-lifetime";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleSizeOverLifetimeHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleSizeOverLifetimeHandle)new DataList();
#else
            var handle = new FluxParticleSizeOverLifetimeHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleSizeOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 現在のモードが終端サイズの操作に対応しているかを返します。
        /// </summary>
        public static bool HasEndSize(this FluxParticleSizeOverLifetimeHandle handle) => handle.HasParameter(FluxParticleSizeOverLifetimeHandle.EndSizeParameterIndex);

        /// <summary>
        /// 終端サイズを取得します。非対応モードでは既定値を返します。
        /// </summary>
        public static float GetEndSize(this FluxParticleSizeOverLifetimeHandle handle) => handle.GetValue(FluxParticleSizeOverLifetimeHandle.EndSizeParameterIndex).x;

        /// <summary>
        /// 終端サイズを変更します。非対応モードではfalseを返します。
        /// </summary>
        public static bool SetEndSize(this FluxParticleSizeOverLifetimeHandle handle, float value) => value >= 0 && handle.SetValue(FluxParticleSizeOverLifetimeHandle.EndSizeParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));
    }
}

