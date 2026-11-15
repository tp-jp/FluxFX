using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// DistanceFadeの設定操作ハンドル。GPU出力のCPU読み戻しは行いません。
    /// </summary>
    public class FluxParticleDistanceFadeHandle : FluxParticleModuleHandle
    {
        internal static FluxParticleDistanceFadeHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleDistanceFadeHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.DistanceFade);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleDistanceFadeHandle>(modules, FluxParticleModuleIds.DistanceFade);
#endif
        }
    }

    /// <summary>
    /// DistanceFadeの型付き設定API。入力検証はExecutorで行います。
    /// </summary>
    public static class FluxParticleDistanceFadeHandleExtensions
    {
        /// <summary>
        /// フェード開始距離を取得します。
        /// </summary>
        public static float GetStartDistance(this FluxParticleDistanceFadeHandle handle) => handle.GetValue((int)FluxParticleDistanceFadeParameter.StartDistance).x;

        /// <summary>
        /// フェード開始距離を変更します。
        /// </summary>
        public static bool SetStartDistance(this FluxParticleDistanceFadeHandle handle, float value) =>
            handle.SetValue((int)FluxParticleDistanceFadeParameter.StartDistance, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// フェード終了距離を取得します。
        /// </summary>
        public static float GetEndDistance(this FluxParticleDistanceFadeHandle handle) => handle.GetValue((int)FluxParticleDistanceFadeParameter.EndDistance).x;

        /// <summary>
        /// フェード終了距離を変更します。
        /// </summary>
        public static bool SetEndDistance(this FluxParticleDistanceFadeHandle handle, float value) =>
            handle.SetValue((int)FluxParticleDistanceFadeParameter.EndDistance, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// フェードの適用先を取得します。
        /// </summary>
        public static FluxParticleDistanceFadeTarget GetTarget(this FluxParticleDistanceFadeHandle handle) => (FluxParticleDistanceFadeTarget)(int)handle.GetValue((int)FluxParticleDistanceFadeParameter.Target).x;

        /// <summary>
        /// フェードの適用先を変更します。
        /// </summary>
        public static bool SetTarget(this FluxParticleDistanceFadeHandle handle, FluxParticleDistanceFadeTarget value) =>
            handle.SetValue((int)FluxParticleDistanceFadeParameter.Target, FluxParticleValueType.Float, new Vector4(System.Convert.ToSingle((int)value), 0, 0, 0));
    }
}
