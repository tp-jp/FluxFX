using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;

namespace TpLab.Flux.FX.Editor.Modules.AudioInput
{
    /// <summary>
    /// AudioInputのLive設定とRuntime APIの契約
    /// </summary>
    public static class FluxParticleAudioInputContract
    {
        /// <summary>
        /// 音域の契約
        /// </summary>
        public static FluxParticleParameterContract<FluxParticleAudioBand> Band { get; } =
            FluxParticleParameter.Enum<FluxParticleAudioBand>("audio-input.band").AllowedValues(FluxParticleAudioBand.Bass, FluxParticleAudioBand.LowMid, FluxParticleAudioBand.HighMid, FluxParticleAudioBand.Treble)
                .ApiName("Band").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 入力感度の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Gain { get; } =
            FluxParticleParameter.Float("audio-input.gain").Range(0, 4)
                .ApiName("Gain").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 無音閾値の契約
        /// </summary>
        public static FluxParticleParameterContract<float> NoiseFloor { get; } =
            FluxParticleParameter.Float("audio-input.noise-floor").Range(0, 0.5f)
                .ApiName("NoiseFloor").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 反応量の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Strength { get; } =
            FluxParticleParameter.Float("audio-input.strength").Range(0, 1)
                .ApiName("Strength").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 立ち上がり時間（秒）の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Attack { get; } =
            FluxParticleParameter.Float("audio-input.attack").Minimum(0.01f)
                .ApiName("Attack").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 減衰時間（秒）の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Release { get; } =
            FluxParticleParameter.Float("audio-input.release").Minimum(0.01f)
                .ApiName("Release").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 遅い応答の追従時間（秒）の契約
        /// </summary>
        public static FluxParticleParameterContract<float> SlowResponse { get; } =
            FluxParticleParameter.Float("audio-input.slow-response").Minimum(0.01f)
                .ApiName("SlowResponse").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 全4音域平均の入力感度の契約
        /// </summary>
        public static FluxParticleParameterContract<float> AverageGain { get; } =
            FluxParticleParameter.Float("audio-input.average-gain").Range(0, 4)
                .ApiName("AverageGain").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// 入力更新の停止判定時間（秒）の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Timeout { get; } =
            FluxParticleParameter.Float("audio-input.timeout").Minimum(0.01f)
                .ApiName("Timeout").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.AudioInput)
                .Handle("FluxParticleAudioInputHandle").Accessor("AudioInput").Enabled("audio-input.enabled")
                .Parameter(FluxParticleAudioInputParameter.Band, Band)
                .Parameter(FluxParticleAudioInputParameter.Gain, Gain)
                .Parameter(FluxParticleAudioInputParameter.NoiseFloor, NoiseFloor)
                .Parameter(FluxParticleAudioInputParameter.Strength, Strength)
                .Parameter(FluxParticleAudioInputParameter.Attack, Attack)
                .Parameter(FluxParticleAudioInputParameter.Release, Release)
                .Parameter(FluxParticleAudioInputParameter.SlowResponse, SlowResponse)
                .Parameter(FluxParticleAudioInputParameter.AverageGain, AverageGain)
                .Parameter(FluxParticleAudioInputParameter.Timeout, Timeout)
                .Build();
    }
}
