using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
namespace TpLab.Flux.FX.Editor.Modules.Noise
{
    /// <summary>
    /// Noiseの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleNoiseContract
    {
        /// <summary>
        /// 強さの契約
        /// </summary>
        public static FluxParticleParameterContract<float> Strength { get; } =
            FluxParticleParameter.Float("noise.strength")
                .ApiName("Strength")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// 空間スケールの契約
        /// </summary>
        public static FluxParticleParameterContract<float> Scale { get; } =
            FluxParticleParameter.Float("noise.scale")
                .ApiName("Scale")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// 時間変化の速度の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Speed { get; } =
            FluxParticleParameter.Float("noise.speed")
                .ApiName("Speed")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("tplab.fluxfx.noise")
                .Handle("FluxParticleNoiseHandle")
                .Accessor("Noise")
                .Enabled("noise.enabled")
                .Parameter(FluxParticleNoiseParameter.Strength, Strength)
                .Parameter(FluxParticleNoiseParameter.Scale, Scale)
                .Parameter(FluxParticleNoiseParameter.Speed, Speed)
                .Build();
    }
}
