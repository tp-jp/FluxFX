using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    /// <summary>
    /// Executor参照、ハンドル固有情報、解決済みParameterスロットを保持します。
    /// </summary>
    /// <remarks>
    /// 利用者はSystem.Main、System.Shape、System.Modulesから取得した型付きハンドルの拡張メソッドを使用します。
    /// 値と入力制約はExecutorが管理します。ハンドルは解決済みスロットへの参照であり、値を複製しません。
    /// ハンドルは所有するSystemの構成が有効な間だけ使用してください。破棄後の操作はできません。
    /// UdonではDataListとして扱うため、継承された操作を非公開にはできません。
    /// newによる直接生成、Add・Clear・インデクサーによる内容変更、別Systemとの共有は行わないでください。
    /// </remarks>
    public class FluxParticleParameterHandle : DataList
    {
        internal const int ExecutorIndex = 0;
        internal const int ContextIndex = 1;
        internal const int ParameterSlotsOffset = 2;

        /// <summary>
        /// ビルド済みBindingをハンドルへ設定します。
        /// </summary>
        internal static void Initialize(DataList handle, DataList binding)
        {
            handle.AddRange(binding);
        }

        /// <summary>
        /// 固定APIのハンドルへExecutor参照とスロットを設定します。
        /// </summary>
        internal static void InitializeSlots(DataList handle, FluxParticleExecutor executor, FluxParticleSystem system, int[] slots)
        {
            handle.Add(new DataToken(executor));
            handle.Add(new DataToken(system));
            for (var i = 0; i < slots.Length; i++)
            {
                handle.Add(slots[i]);
            }
        }
    }

    /// <summary>
    /// カスタムハンドル実装向けのParameter操作
    /// </summary>
    /// <remarks>
    /// parameterには契約と共有するenumの値を使用します。Executorのスロット番号やShader Property IDではありません。
    /// 各メソッドはnullでない取得済みハンドルを対象とします。
    /// </remarks>
    public static class FluxParticleParameterHandleExtensions
    {
        /// <summary>
        /// 現在の構成がParameterの操作に対応しているかを返します。
        /// </summary>
        /// <remarks>
        /// ビルド済み構成での対応の有無です。ModuleのON/OFFや現在変更可能かどうかは表しません。
        /// </remarks>
        public static bool HasParameter(this FluxParticleParameterHandle handle, int parameter)
        {
            return parameter >= 0 && parameter < handle.Count - FluxParticleParameterHandle.ParameterSlotsOffset &&
                handle[parameter + FluxParticleParameterHandle.ParameterSlotsOffset].Int >= 0;
        }

        /// <summary>
        /// 現在値を取得します。非対応のParameterはVector4.zeroを返します。
        /// </summary>
        /// <remarks>
        /// 値が0のParameterと非対応のParameterは戻り値で区別できません。対応の有無はHasParameterで確認します。
        /// </remarks>
        public static Vector4 GetValue(this FluxParticleParameterHandle handle, int parameter)
        {
            if (!handle.HasParameter(parameter)) return Vector4.zero;
            return ((FluxParticleExecutor)handle[FluxParticleParameterHandle.ExecutorIndex].Reference)
                .GetParameterValue(handle[parameter + FluxParticleParameterHandle.ParameterSlotsOffset].Int);
        }

        /// <summary>
        /// Executorの型・更新Policy・入力制約の検証を通して値を変更します。
        /// </summary>
        /// <returns>変更できた場合はtrue。非対応・型不一致・制約違反の場合は値を保持してfalse</returns>
        /// <remarks>
        /// Static Parameterはシミュレーション開始後に変更できません。Executorを直接操作した場合も同じ検証が適用されます。
        /// </remarks>
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
