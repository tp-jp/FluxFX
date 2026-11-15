using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Brightness
{
    /// <summary>
    /// BrightnessのLive設定とRuntime APIの契約
    /// </summary>
    public static class FluxParticleBrightnessContract
    {
        /// <summary>
        /// 基準倍率の下限の契約
        /// </summary>
        public static FluxParticleParameterContract<float> BaseMinimum { get; } =
            FluxParticleParameter.Float("brightness.base-minimum").Minimum(0)
                .ApiName("BaseMinimum").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 基準倍率の上限の契約
        /// </summary>
        public static FluxParticleParameterContract<float> BaseMaximum { get; } =
            FluxParticleParameter.Float("brightness.base-maximum").Minimum(0)
                .ApiName("BaseMaximum").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 時間変化の波形の契約
        /// </summary>
        public static FluxParticleParameterContract<FluxParticleBrightnessWaveform> Waveform { get; } =
            FluxParticleParameter.Enum<FluxParticleBrightnessWaveform>("brightness.waveform").AllowedValues(FluxParticleBrightnessWaveform.Off, FluxParticleBrightnessWaveform.Sine, FluxParticleBrightnessWaveform.Pulse)
                .ApiName("Waveform").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 基準周期（秒）。0は時間変化なしの契約
        /// </summary>
        public static FluxParticleParameterContract<float> Period { get; } =
            FluxParticleParameter.Float("brightness.period").Minimum(0)
                .ApiName("Period").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 粒ごとの周期倍率の下限の契約
        /// </summary>
        public static FluxParticleParameterContract<float> PeriodMinimum { get; } =
            FluxParticleParameter.Float("brightness.period-minimum").Minimum(0.0001f)
                .ApiName("PeriodMinimum").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 粒ごとの周期倍率の上限の契約
        /// </summary>
        public static FluxParticleParameterContract<float> PeriodMaximum { get; } =
            FluxParticleParameter.Float("brightness.period-maximum").Minimum(0.0001f)
                .ApiName("PeriodMaximum").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 位相のランダム化量の契約
        /// </summary>
        public static FluxParticleParameterContract<float> RandomPhase { get; } =
            FluxParticleParameter.Float("brightness.random-phase").Range(0, 1)
                .ApiName("RandomPhase").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 時間基準の契約
        /// </summary>
        public static FluxParticleParameterContract<FluxParticleBrightnessTimeMode> TimeMode { get; } =
            FluxParticleParameter.Enum<FluxParticleBrightnessTimeMode>("brightness.time-mode").AllowedValues(FluxParticleBrightnessTimeMode.SystemTime, FluxParticleBrightnessTimeMode.ParticleAge)
                .ApiName("TimeMode").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 時間変化倍率の下限の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Minimum { get; } =
            FluxParticleParameter.Float("brightness.minimum").Minimum(0)
                .ApiName("Minimum").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 時間変化倍率の上限の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Maximum { get; } =
            FluxParticleParameter.Float("brightness.maximum").Minimum(0)
                .ApiName("Maximum").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// Pulseの点灯割合の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Duty { get; } =
            FluxParticleParameter.Float("brightness.duty").Range(0, 1)
                .ApiName("Duty").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// Pulseの切替の滑らかさの契約
        /// </summary>
        public static FluxParticleParameterContract<float> Smoothness { get; } =
            FluxParticleParameter.Float("brightness.smoothness").Range(0, 1)
                .ApiName("Smoothness").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 倍率の適用先の契約
        /// </summary>
        public static FluxParticleParameterContract<FluxParticleBrightnessTarget> Target { get; } =
            FluxParticleParameter.Enum<FluxParticleBrightnessTarget>("brightness.target").AllowedValues(FluxParticleBrightnessTarget.Rgb, FluxParticleBrightnessTarget.Alpha, FluxParticleBrightnessTarget.Both)
                .ApiName("Target").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 音声増光量の契約
        /// </summary>
        public static FluxParticleParameterContract<float> AudioBoost { get; } =
            FluxParticleParameter.Float("brightness.audio-boost").Minimum(0)
                .ApiName("AudioBoost").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 音声の立ち上がり時間（秒）の契約
        /// </summary>
        public static FluxParticleParameterContract<float> AudioAttack { get; } =
            FluxParticleParameter.Float("brightness.audio-attack").Minimum(0.01f)
                .ApiName("AudioAttack").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 音声の減衰時間（秒）の契約
        /// </summary>
        public static FluxParticleParameterContract<float> AudioRelease { get; } =
            FluxParticleParameter.Float("brightness.audio-release").Minimum(0.01f)
                .ApiName("AudioRelease").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.Brightness)
                .Handle("FluxParticleBrightnessHandle").Accessor("Brightness").Enabled("brightness.enabled")
                .Parameter(FluxParticleBrightnessParameter.BaseMinimum, BaseMinimum)
                .Parameter(FluxParticleBrightnessParameter.BaseMaximum, BaseMaximum)
                .Parameter(FluxParticleBrightnessParameter.Waveform, Waveform)
                .Parameter(FluxParticleBrightnessParameter.Period, Period)
                .Parameter(FluxParticleBrightnessParameter.PeriodMinimum, PeriodMinimum)
                .Parameter(FluxParticleBrightnessParameter.PeriodMaximum, PeriodMaximum)
                .Parameter(FluxParticleBrightnessParameter.RandomPhase, RandomPhase)
                .Parameter(FluxParticleBrightnessParameter.TimeMode, TimeMode)
                .Parameter(FluxParticleBrightnessParameter.Minimum, Minimum)
                .Parameter(FluxParticleBrightnessParameter.Maximum, Maximum)
                .Parameter(FluxParticleBrightnessParameter.Duty, Duty)
                .Parameter(FluxParticleBrightnessParameter.Smoothness, Smoothness)
                .Parameter(FluxParticleBrightnessParameter.Target, Target)
                .Parameter(FluxParticleBrightnessParameter.AudioBoost, AudioBoost)
                .Parameter(FluxParticleBrightnessParameter.AudioAttack, AudioAttack)
                .Parameter(FluxParticleBrightnessParameter.AudioRelease, AudioRelease)
                .Build();
    }
}
