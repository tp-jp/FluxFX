using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Blinkの操作ハンドル
    /// </summary>
    public class FluxParticleBlinkHandle : FluxParticleModuleHandle
    {
        internal const int FrequencyParameterIndex = 0;
        internal const int DutyParameterIndex = 1;
        internal const int MinimumParameterIndex = 2;
        internal const int SmoothnessParameterIndex = 3;
        internal const int RandomPhaseParameterIndex = 4;
        internal const int TimeModeParameterIndex = 5;
        internal const int TargetParameterIndex = 6;

        /// <summary>
        /// Blinkのハンドルを取得し、初回のみ生成します。
        /// </summary>
        public static FluxParticleBlinkHandle Get(FluxParticleModules modules)
        {
            const string id = "tplab.fluxfx.blink";
            var cached = modules.GetCachedHandle(id);
            if (cached != null) return (FluxParticleBlinkHandle)cached;
            var binding = modules.GetBinding(id);
            if (binding == null) return null;
#if COMPILER_UDONSHARP
            var handle = (FluxParticleBlinkHandle)new DataList();
#else
            var handle = new FluxParticleBlinkHandle();
#endif
            Initialize(handle, binding);
            modules.CacheHandle(id, handle);
            return handle;
        }
    }

    public static class FluxParticleBlinkHandleExtensions
    {
        /// <summary>
        /// 周波数を取得します。
        /// </summary>
        public static float GetFrequency(this FluxParticleBlinkHandle handle) => handle.GetValue(FluxParticleBlinkHandle.FrequencyParameterIndex).x;

        /// <summary>
        /// 周波数を変更します。
        /// </summary>
        public static bool SetFrequency(this FluxParticleBlinkHandle handle, float value) => value >= 0 && handle.SetValue(FluxParticleBlinkHandle.FrequencyParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 点灯時間の割合を取得します。
        /// </summary>
        public static float GetDuty(this FluxParticleBlinkHandle handle) => handle.GetValue(FluxParticleBlinkHandle.DutyParameterIndex).x;

        /// <summary>
        /// 点灯時間の割合を変更します。
        /// </summary>
        public static bool SetDuty(this FluxParticleBlinkHandle handle, float value) => value >= 0 && value <= 1 && handle.SetValue(FluxParticleBlinkHandle.DutyParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 消灯時の倍率を取得します。
        /// </summary>
        public static float GetMinimum(this FluxParticleBlinkHandle handle) => handle.GetValue(FluxParticleBlinkHandle.MinimumParameterIndex).x;

        /// <summary>
        /// 消灯時の倍率を変更します。
        /// </summary>
        public static bool SetMinimum(this FluxParticleBlinkHandle handle, float value) => value >= 0 && value <= 1 && handle.SetValue(FluxParticleBlinkHandle.MinimumParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 切り替わりの滑らかさを取得します。
        /// </summary>
        public static float GetSmoothness(this FluxParticleBlinkHandle handle) => handle.GetValue(FluxParticleBlinkHandle.SmoothnessParameterIndex).x;

        /// <summary>
        /// 切り替わりの滑らかさを変更します。
        /// </summary>
        public static bool SetSmoothness(this FluxParticleBlinkHandle handle, float value) => value >= 0 && value <= 1 && handle.SetValue(FluxParticleBlinkHandle.SmoothnessParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 位相のランダム化量を取得します。
        /// </summary>
        public static float GetRandomPhase(this FluxParticleBlinkHandle handle) => handle.GetValue(FluxParticleBlinkHandle.RandomPhaseParameterIndex).x;

        /// <summary>
        /// 位相のランダム化量を変更します。
        /// </summary>
        public static bool SetRandomPhase(this FluxParticleBlinkHandle handle, float value) => value >= 0 && value <= 1 && handle.SetValue(FluxParticleBlinkHandle.RandomPhaseParameterIndex, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 時間の基準を取得します。
        /// </summary>
        public static FluxParticleBlinkTimeMode GetTimeMode(this FluxParticleBlinkHandle handle) => (FluxParticleBlinkTimeMode)(int)handle.GetValue(FluxParticleBlinkHandle.TimeModeParameterIndex).x;

        /// <summary>
        /// 時間の基準を変更します。
        /// </summary>
        public static bool SetTimeMode(this FluxParticleBlinkHandle handle, FluxParticleBlinkTimeMode value) => (int)value >= 0 && (int)value <= 1 && handle.SetValue(FluxParticleBlinkHandle.TimeModeParameterIndex, FluxParticleValueType.Float, new Vector4(value == FluxParticleBlinkTimeMode.ParticleAge ? 1f : 0f, 0f, 0f, 0f));

        /// <summary>
        /// 補正対象を取得します。
        /// </summary>
        public static FluxParticleBlinkTarget GetTarget(this FluxParticleBlinkHandle handle) => (FluxParticleBlinkTarget)(int)handle.GetValue(FluxParticleBlinkHandle.TargetParameterIndex).x;

        /// <summary>
        /// 補正対象を変更します。
        /// </summary>
        public static bool SetTarget(this FluxParticleBlinkHandle handle, FluxParticleBlinkTarget value) => (int)value >= 0 && (int)value <= 2 && handle.SetValue(FluxParticleBlinkHandle.TargetParameterIndex, FluxParticleValueType.Float, new Vector4(value == FluxParticleBlinkTarget.Both ? 2f : value == FluxParticleBlinkTarget.Alpha ? 1f : 0f, 0f, 0f, 0f));

    }
}
