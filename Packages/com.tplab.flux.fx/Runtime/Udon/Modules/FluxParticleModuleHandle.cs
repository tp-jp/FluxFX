using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// 標準・カスタムModuleの操作ハンドルの基底型
    /// </summary>
    /// <remarks>
    /// カスタムハンドルはこの型を継承し、Factoryで取得します。Parameterの操作は拡張メソッドで定義します。
    /// HasEnabledはEnabledの対応の有無、GetEnabledは現在の有効状態です。OFFでもハンドルとParameterは保持されます。
    /// </remarks>
    public class FluxParticleModuleHandle : FluxParticleParameterHandle
    {
        internal const int EnabledSlotIndex = ContextIndex;
    }

    public static class FluxParticleModuleHandleExtensions
    {
        /// <summary>
        /// Enabledの操作に対応しているかを返します。
        /// </summary>
        public static bool HasEnabled(this FluxParticleModuleHandle handle) => handle[FluxParticleModuleHandle.EnabledSlotIndex].Int >= 0;

        /// <summary>
        /// Moduleの有効状態を取得します。Enabled非対応の場合はtrueを返します。
        /// </summary>
        public static bool GetEnabled(this FluxParticleModuleHandle handle)
        {
            return !handle.HasEnabled() || ((FluxParticleExecutor)handle[FluxParticleModuleHandle.ExecutorIndex].Reference).GetParameterValue(handle[FluxParticleModuleHandle.EnabledSlotIndex].Int).x != 0;
        }

        /// <summary>
        /// Moduleの有効状態を変更します。
        /// </summary>
        /// <returns>変更できた場合はtrue。Enabled非対応の場合はfalse</returns>
        public static bool SetEnabled(this FluxParticleModuleHandle handle, bool enabled)
        {
            return handle.HasEnabled() && ((FluxParticleExecutor)handle[FluxParticleModuleHandle.ExecutorIndex].Reference)
                .SetParameterValue(handle[FluxParticleModuleHandle.EnabledSlotIndex].Int, (int)FluxParticleValueType.Float, new Vector4(enabled ? 1 : 0, 0, 0, 0));
        }
    }
}
