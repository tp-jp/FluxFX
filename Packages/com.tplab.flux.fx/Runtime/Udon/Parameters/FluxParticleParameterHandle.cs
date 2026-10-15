using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// Executor参照、ハンドル固有情報、解決済みParameterスロットを保持します。
    /// </summary>
    public class FluxParticleParameterHandle : DataList
    {
        internal const int ExecutorIndex = 0;
        internal const int ContextIndex = 1;
        internal const int ParameterSlotsOffset = 2;

        /// <summary>
        /// ビルド済みBindingをハンドルへ設定します。
        /// </summary>
        public static void Initialize(DataList handle, DataList binding)
        {
            handle.AddRange(binding);
        }

        /// <summary>
        /// 固定APIのハンドルへExecutor参照とスロットを設定します。
        /// </summary>
        public static void InitializeSlots(DataList handle, FluxParticleExecutor executor, FluxParticleSystem system, int[] slots)
        {
            handle.Add(new DataToken(executor));
            handle.Add(new DataToken(system));
            for (var i = 0; i < slots.Length; i++)
            {
                handle.Add(slots[i]);
            }
        }
    }

    public static class FluxParticleParameterHandleExtensions
    {
        /// <summary>
        /// 現在の構成がParameterの操作に対応しているかを返します。
        /// </summary>
        public static bool HasParameter(this FluxParticleParameterHandle handle, int parameter)
        {
            return parameter >= 0 && parameter < handle.Count - FluxParticleParameterHandle.ParameterSlotsOffset &&
                handle[parameter + FluxParticleParameterHandle.ParameterSlotsOffset].Int >= 0;
        }

        /// <summary>
        /// 現在値を取得します。非対応のParameterは既定値を返します。
        /// </summary>
        public static Vector4 GetValue(this FluxParticleParameterHandle handle, int parameter)
        {
            if (!handle.HasParameter(parameter)) return Vector4.zero;
            return ((FluxParticleExecutor)handle[FluxParticleParameterHandle.ExecutorIndex].Reference)
                .GetParameterValue(handle[parameter + FluxParticleParameterHandle.ParameterSlotsOffset].Int);
        }

        /// <summary>
        /// Executorの型・Static制約・有限値の検証を通して値を変更します。
        /// </summary>
        public static bool SetValue(this FluxParticleParameterHandle handle, int parameter, FluxParticleValueType type, Vector4 value)
        {
            if (!handle.HasParameter(parameter)) return false;
            return ((FluxParticleExecutor)handle[FluxParticleParameterHandle.ExecutorIndex].Reference)
                .SetParameterValue(handle[parameter + FluxParticleParameterHandle.ParameterSlotsOffset].Int, (int)type, value);
        }

        /// <summary>
        /// 2つの値を事前検証し、両方とも変更できる場合に設定します。
        /// </summary>
        public static bool SetPair(this FluxParticleParameterHandle handle, int first, int second, FluxParticleValueType type, Vector4 min, Vector4 max)
        {
            if (!handle.HasParameter(first) || !handle.HasParameter(second)) return false;
            var executor = (FluxParticleExecutor)handle[FluxParticleParameterHandle.ExecutorIndex].Reference;
            var firstSlot = handle[first + FluxParticleParameterHandle.ParameterSlotsOffset].Int;
            var secondSlot = handle[second + FluxParticleParameterHandle.ParameterSlotsOffset].Int;
            if (!executor.CanSetParameterValue(firstSlot, (int)type, min) || !executor.CanSetParameterValue(secondSlot, (int)type, max)) return false;
            executor.SetParameterValue(firstSlot, (int)type, min);
            executor.SetParameterValue(secondSlot, (int)type, max);
            return true;
        }
    }
}
