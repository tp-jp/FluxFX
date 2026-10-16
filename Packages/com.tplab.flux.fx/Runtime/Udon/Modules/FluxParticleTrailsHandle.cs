using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Trailsの操作ハンドル
    /// </summary>
    public class FluxParticleTrailsHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Trailsのハンドルを取得し、初回のみ生成します。
        /// </summary>
        internal static FluxParticleTrailsHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleTrailsHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.Trails);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleTrailsHandle>(modules, FluxParticleModuleIds.Trails);
#endif
        }
    }

    public static class FluxParticleTrailsHandleExtensions
    {
        /// <summary>
        /// 軌跡の記録時間を取得します。
        /// </summary>
        public static float GetDuration(this FluxParticleTrailsHandle handle) => handle.GetValue((int)FluxParticleTrailsParameter.Duration).x;

        /// <summary>
        /// 軌跡の記録時間を変更し、有効な履歴を消去します。
        /// </summary>
        /// <param name="value">正の記録時間（秒）</param>
        public static bool SetDuration(this FluxParticleTrailsHandle handle, float value) => handle.SetValue((int)FluxParticleTrailsParameter.Duration, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 軌跡の幅を取得します。
        /// </summary>
        public static float GetWidth(this FluxParticleTrailsHandle handle) => handle.GetValue((int)FluxParticleTrailsParameter.Width).x;

        /// <summary>
        /// 軌跡の幅を変更します。
        /// </summary>
        public static bool SetWidth(this FluxParticleTrailsHandle handle, float value) => handle.SetValue((int)FluxParticleTrailsParameter.Width, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 軌跡の色を取得します。
        /// </summary>
        public static Color GetColor(this FluxParticleTrailsHandle handle) => handle.GetValue((int)FluxParticleTrailsParameter.Color);

        /// <summary>
        /// 軌跡の色を変更します。
        /// </summary>
        public static bool SetColor(this FluxParticleTrailsHandle handle, Color value) => handle.SetValue((int)FluxParticleTrailsParameter.Color, FluxParticleValueType.Float4, value);
    }
}
