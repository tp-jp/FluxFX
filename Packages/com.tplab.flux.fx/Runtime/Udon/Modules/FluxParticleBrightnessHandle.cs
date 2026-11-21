using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;

namespace TpLab.Flux.FX.Udon.Modules
{
    /// <summary>
    /// Brightnessの操作ハンドル
    /// </summary>
    public class FluxParticleBrightnessHandle : FluxParticleModuleHandle
    {
        /// <summary>
        /// Systemごとの操作ハンドルを取得します。
        /// </summary>
        internal static FluxParticleBrightnessHandle Get(FluxParticleModules modules)
        {
#if COMPILER_UDONSHARP
            return (FluxParticleBrightnessHandle)FluxParticleModuleHandleFactory.Get(modules, FluxParticleModuleIds.Brightness);
#else
            return FluxParticleModuleHandleFactory.Get<FluxParticleBrightnessHandle>(modules, FluxParticleModuleIds.Brightness);
#endif
        }
    }

    /// <summary>
    /// 入力検証をExecutorへ委ねるBrightnessの薄い操作API
    /// </summary>
    public static class FluxParticleBrightnessHandleExtensions
    {
        /// <summary>
        /// 未平滑化の入力元を取得します。
        /// </summary>
        public static FluxParticleAudioSource GetAudioSource(this FluxParticleBrightnessHandle handle) =>
            (FluxParticleAudioSource)(int)handle.GetValue((int)FluxParticleBrightnessParameter.AudioSource).x;

        /// <summary>
        /// 独立した応答へ渡す入力元を変更します。
        /// </summary>
        public static bool SetAudioSource(this FluxParticleBrightnessHandle handle, FluxParticleAudioSource value) =>
            handle.SetValue((int)FluxParticleBrightnessParameter.AudioSource, FluxParticleValueType.Float,
                new Vector4(System.Convert.ToSingle((int)value), 0, 0, 0));

        /// <summary>
        /// 基準倍率の下限を取得します。
        /// </summary>
        public static float GetBaseMinimum(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.BaseMinimum).x;

        /// <summary>
        /// 基準倍率の下限を変更します。
        /// </summary>
        public static bool SetBaseMinimum(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.BaseMinimum, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 基準倍率の上限を取得します。
        /// </summary>
        public static float GetBaseMaximum(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.BaseMaximum).x;

        /// <summary>
        /// 基準倍率の上限を変更します。
        /// </summary>
        public static bool SetBaseMaximum(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.BaseMaximum, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 時間変化の波形を取得します。
        /// </summary>
        public static FluxParticleBrightnessWaveform GetWaveform(this FluxParticleBrightnessHandle handle) => (FluxParticleBrightnessWaveform)(int)handle.GetValue((int)FluxParticleBrightnessParameter.Waveform).x;

        /// <summary>
        /// 時間変化の波形を変更します。
        /// </summary>
        public static bool SetWaveform(this FluxParticleBrightnessHandle handle, FluxParticleBrightnessWaveform value) => handle.SetValue((int)FluxParticleBrightnessParameter.Waveform, FluxParticleValueType.Float, new Vector4(System.Convert.ToSingle((int)value), 0, 0, 0));

        /// <summary>
        /// 基準周期（秒）。0は時間変化なしを取得します。
        /// </summary>
        public static float GetPeriod(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.Period).x;

        /// <summary>
        /// 基準周期（秒）。0は時間変化なしを変更します。
        /// </summary>
        public static bool SetPeriod(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.Period, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 粒ごとの周期倍率の下限を取得します。
        /// </summary>
        public static float GetPeriodMinimum(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.PeriodMinimum).x;

        /// <summary>
        /// 粒ごとの周期倍率の下限を変更します。
        /// </summary>
        public static bool SetPeriodMinimum(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.PeriodMinimum, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 粒ごとの周期倍率の上限を取得します。
        /// </summary>
        public static float GetPeriodMaximum(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.PeriodMaximum).x;

        /// <summary>
        /// 粒ごとの周期倍率の上限を変更します。
        /// </summary>
        public static bool SetPeriodMaximum(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.PeriodMaximum, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 位相のランダム化量を取得します。
        /// </summary>
        public static float GetRandomPhase(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.RandomPhase).x;

        /// <summary>
        /// 位相のランダム化量を変更します。
        /// </summary>
        public static bool SetRandomPhase(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.RandomPhase, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 時間基準を取得します。
        /// </summary>
        public static FluxParticleBrightnessTimeMode GetTimeMode(this FluxParticleBrightnessHandle handle) => (FluxParticleBrightnessTimeMode)(int)handle.GetValue((int)FluxParticleBrightnessParameter.TimeMode).x;

        /// <summary>
        /// 時間基準を変更します。
        /// </summary>
        public static bool SetTimeMode(this FluxParticleBrightnessHandle handle, FluxParticleBrightnessTimeMode value) => handle.SetValue((int)FluxParticleBrightnessParameter.TimeMode, FluxParticleValueType.Float, new Vector4(System.Convert.ToSingle((int)value), 0, 0, 0));

        /// <summary>
        /// 時間変化倍率の下限を取得します。
        /// </summary>
        public static float GetMinimum(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.Minimum).x;

        /// <summary>
        /// 時間変化倍率の下限を変更します。
        /// </summary>
        public static bool SetMinimum(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.Minimum, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 時間変化倍率の上限を取得します。
        /// </summary>
        public static float GetMaximum(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.Maximum).x;

        /// <summary>
        /// 時間変化倍率の上限を変更します。
        /// </summary>
        public static bool SetMaximum(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.Maximum, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// Pulseの点灯割合を取得します。
        /// </summary>
        public static float GetDuty(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.Duty).x;

        /// <summary>
        /// Pulseの点灯割合を変更します。
        /// </summary>
        public static bool SetDuty(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.Duty, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// Pulseの切替の滑らかさを取得します。
        /// </summary>
        public static float GetSmoothness(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.Smoothness).x;

        /// <summary>
        /// Pulseの切替の滑らかさを変更します。
        /// </summary>
        public static bool SetSmoothness(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.Smoothness, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 倍率の適用先を取得します。
        /// </summary>
        public static FluxParticleBrightnessTarget GetTarget(this FluxParticleBrightnessHandle handle) => (FluxParticleBrightnessTarget)(int)handle.GetValue((int)FluxParticleBrightnessParameter.Target).x;

        /// <summary>
        /// 倍率の適用先を変更します。
        /// </summary>
        public static bool SetTarget(this FluxParticleBrightnessHandle handle, FluxParticleBrightnessTarget value) => handle.SetValue((int)FluxParticleBrightnessParameter.Target, FluxParticleValueType.Float, new Vector4(System.Convert.ToSingle((int)value), 0, 0, 0));

        /// <summary>
        /// 音声増光量を取得します。
        /// </summary>
        public static float GetAudioBoost(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.AudioBoost).x;

        /// <summary>
        /// 音声増光量を変更します。
        /// </summary>
        public static bool SetAudioBoost(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.AudioBoost, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 音声の立ち上がり時間（秒）を取得します。
        /// </summary>
        public static float GetAudioAttack(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.AudioAttack).x;

        /// <summary>
        /// 音声の立ち上がり時間（秒）を変更します。
        /// </summary>
        public static bool SetAudioAttack(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.AudioAttack, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

        /// <summary>
        /// 音声の減衰時間（秒）を取得します。
        /// </summary>
        public static float GetAudioRelease(this FluxParticleBrightnessHandle handle) => handle.GetValue((int)FluxParticleBrightnessParameter.AudioRelease).x;

        /// <summary>
        /// 音声の減衰時間（秒）を変更します。
        /// </summary>
        public static bool SetAudioRelease(this FluxParticleBrightnessHandle handle, float value) => handle.SetValue((int)FluxParticleBrightnessParameter.AudioRelease, FluxParticleValueType.Float, new Vector4(value, 0, 0, 0));

    }
}
