using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// ColorOverLifetimeの操作ハンドル
    /// </summary>
    public class FluxParticleColorOverLifetimeHandle : FluxParticleModuleHandle
    {
        internal const int EndColorParameterIndex = 0;

        /// <summary>
        /// ColorOverLifetimeのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleColorOverLifetimeHandle Get(FluxParticleModules modules)
        {
            const string id = "fluxfx.color-over-lifetime";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleColorOverLifetimeHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleColorOverLifetimeHandle)new DataList();
#else
            var handle = new FluxParticleColorOverLifetimeHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleColorOverLifetimeHandleExtensions
    {
        /// <summary>
        /// 現在のモードが終端色の操作に対応しているかを返します。
        /// </summary>
        public static bool HasEndColor(this FluxParticleColorOverLifetimeHandle handle) => handle.HasParameter(FluxParticleColorOverLifetimeHandle.EndColorParameterIndex);

        /// <summary>
        /// 終端色を取得します。非対応モードでは既定値を返します。
        /// </summary>
        public static Color GetEndColor(this FluxParticleColorOverLifetimeHandle handle) => handle.GetValue(FluxParticleColorOverLifetimeHandle.EndColorParameterIndex);

        /// <summary>
        /// 終端色を変更します。非対応モードではfalseを返します。
        /// </summary>
        public static bool SetEndColor(this FluxParticleColorOverLifetimeHandle handle, Color value) => handle.SetValue(FluxParticleColorOverLifetimeHandle.EndColorParameterIndex, FluxParticleValueType.Float4, value);
    }
}

