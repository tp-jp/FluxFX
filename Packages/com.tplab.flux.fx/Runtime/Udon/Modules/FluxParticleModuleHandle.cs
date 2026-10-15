using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Executor参照、Enabledスロット、ローカル番号順のParameterスロットを保持します。
    /// </summary>
    public class FluxParticleModuleHandle : DataList
    {
        internal const int ExecutorIndex = 0;
        internal const int EnabledSlotIndex = 1;
        internal const int ParameterSlotsOffset = 2;

        /// <summary>
        /// ビルド済みBindingをハンドルへ設定します。
        /// </summary>
        public static void Initialize(DataList handle, DataList binding)
        {
            handle.AddRange(binding);
        }
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

        /// <summary>
        /// ローカル番号に対応する現在値を取得します。
        /// </summary>
        public static Vector4 GetValue(this FluxParticleModuleHandle handle, int parameter)
        {
            return ((FluxParticleExecutor)handle[FluxParticleModuleHandle.ExecutorIndex].Reference).GetParameterValue(handle[parameter + FluxParticleModuleHandle.ParameterSlotsOffset].Int);
        }

        /// <summary>
        /// ローカル番号に対応する値をExecutorの検証を通して変更します。
        /// </summary>
        public static bool SetValue(this FluxParticleModuleHandle handle, int parameter, FluxParticleValueType type, Vector4 value)
        {
            return ((FluxParticleExecutor)handle[FluxParticleModuleHandle.ExecutorIndex].Reference).SetParameterValue(handle[parameter + FluxParticleModuleHandle.ParameterSlotsOffset].Int, (int)type, value);
        }
    }
}
