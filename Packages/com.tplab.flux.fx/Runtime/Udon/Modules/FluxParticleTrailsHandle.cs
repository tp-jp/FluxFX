using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Trailsの操作ハンドル
    /// </summary>
    public class FluxParticleTrailsHandle : FluxParticleModuleHandle
    {
        internal const int WidthParameterIndex = 0;
        internal const int ColorParameterIndex = 1;

        /// <summary>
        /// Trailsのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleTrailsHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.trails";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleTrailsHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleTrailsHandle)new DataList();
#else
            var handle = new FluxParticleTrailsHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleTrailsHandleExtensions
    {
        /// <summary>
        /// 軌跡の幅を取得します。
        /// </summary>
        public static float GetWidth(this FluxParticleTrailsHandle handle) => handle.GetValue(FluxParticleTrailsHandle.WidthParameterIndex).x;

        /// <summary>
        /// 軌跡の幅を変更します。
        /// </summary>
        public static bool SetWidth(this FluxParticleTrailsHandle handle, float value) => value >= 0 && handle.SetValue(FluxParticleTrailsHandle.WidthParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 軌跡の色を取得します。
        /// </summary>
        public static Color GetColor(this FluxParticleTrailsHandle handle) => handle.GetValue(FluxParticleTrailsHandle.ColorParameterIndex);

        /// <summary>
        /// 軌跡の色を変更します。
        /// </summary>
        public static bool SetColor(this FluxParticleTrailsHandle handle, Color value) => handle.SetValue(FluxParticleTrailsHandle.ColorParameterIndex, FluxParticleValueType.Float4, value);
    }
}
