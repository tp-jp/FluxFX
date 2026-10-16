using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.Blink
{
    /// <summary>
    /// Blinkの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleBlinkContract
    {
        /// <summary>
        /// 周波数の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Frequency { get; } =
            FluxParticleParameter.Float("blink.frequency")
                .ApiName("Frequency")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// 点灯時間の割合の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Duty { get; } =
            FluxParticleParameter.Float("blink.duty")
                .ApiName("Duty")
                .Policy(FluxParticleParameterPolicy.Live)
                .Range(0, 1)
                .Build();

        /// <summary>
        /// 消灯時の倍率の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Minimum { get; } =
            FluxParticleParameter.Float("blink.minimum")
                .ApiName("Minimum")
                .Policy(FluxParticleParameterPolicy.Live)
                .Range(0, 1)
                .Build();

        /// <summary>
        /// 切り替わりの滑らかさの契約
        /// </summary>
        public static FluxParticleParameterContract<float> Smoothness { get; } =
            FluxParticleParameter.Float("blink.smoothness")
                .ApiName("Smoothness")
                .Policy(FluxParticleParameterPolicy.Live)
                .Range(0, 1)
                .Build();

        /// <summary>
        /// 位相のランダム化量の契約
        /// </summary>
        public static FluxParticleParameterContract<float> RandomPhase { get; } =
            FluxParticleParameter.Float("blink.random-phase")
                .ApiName("RandomPhase")
                .Policy(FluxParticleParameterPolicy.Live)
                .Range(0, 1)
                .Build();

        /// <summary>
        /// 時間の基準の契約
        /// </summary>
        public static FluxParticleParameterContract<FluxParticleBlinkTimeMode> TimeMode { get; } =
            FluxParticleParameter.Enum<FluxParticleBlinkTimeMode>("blink.time-mode")
                .ApiName("TimeMode")
                .Policy(FluxParticleParameterPolicy.Live)
                .AllowedValues(FluxParticleBlinkTimeMode.SystemTime, FluxParticleBlinkTimeMode.ParticleAge)
                .Build();

        /// <summary>
        /// 補正対象の契約
        /// </summary>
        public static FluxParticleParameterContract<FluxParticleBlinkTarget> Target { get; } =
            FluxParticleParameter.Enum<FluxParticleBlinkTarget>("blink.target")
                .ApiName("Target")
                .Policy(FluxParticleParameterPolicy.Live)
                .AllowedValues(FluxParticleBlinkTarget.Rgb, FluxParticleBlinkTarget.Alpha, FluxParticleBlinkTarget.Both)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("tplab.fluxfx.blink")
                .Handle("FluxParticleBlinkHandle")
                .Accessor("Blink")
                .Enabled("blink.enabled")
                .Parameter(FluxParticleBlinkParameter.Frequency, Frequency)
                .Parameter(FluxParticleBlinkParameter.Duty, Duty)
                .Parameter(FluxParticleBlinkParameter.Minimum, Minimum)
                .Parameter(FluxParticleBlinkParameter.Smoothness, Smoothness)
                .Parameter(FluxParticleBlinkParameter.RandomPhase, RandomPhase)
                .Parameter(FluxParticleBlinkParameter.TimeMode, TimeMode)
                .Parameter(FluxParticleBlinkParameter.Target, Target)
                .Build();
    }
}
