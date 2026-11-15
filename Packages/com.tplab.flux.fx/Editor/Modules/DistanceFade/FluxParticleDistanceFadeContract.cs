using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;

namespace TpLab.Flux.FX.Editor.Modules.DistanceFade
{
    /// <summary>
    /// DistanceFadeのLive設定とRuntime APIの契約
    /// </summary>
    public static class FluxParticleDistanceFadeContract
    {
        /// <summary>
        /// フェード開始距離の契約
        /// </summary>
        public static FluxParticleParameterContract<float> StartDistance { get; } =
            FluxParticleParameter.Float("distance-fade.start-distance").Minimum(0)
                .ApiName("StartDistance").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// フェード終了距離の契約
        /// </summary>
        public static FluxParticleParameterContract<float> EndDistance { get; } =
            FluxParticleParameter.Float("distance-fade.end-distance").Minimum(0)
                .ApiName("EndDistance").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// フェードの適用先の契約
        /// </summary>
        public static FluxParticleParameterContract<FluxParticleDistanceFadeTarget> Target { get; } =
            FluxParticleParameter.Enum<FluxParticleDistanceFadeTarget>("distance-fade.target").AllowedValues(FluxParticleDistanceFadeTarget.Rgb, FluxParticleDistanceFadeTarget.Alpha)
                .ApiName("Target").Policy(FluxParticleParameterPolicy.Live).Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.DistanceFade)
                .Handle("FluxParticleDistanceFadeHandle").Accessor("DistanceFade").Enabled("distance-fade.enabled")
                .Parameter(FluxParticleDistanceFadeParameter.StartDistance, StartDistance)
                .Parameter(FluxParticleDistanceFadeParameter.EndDistance, EndDistance)
                .Parameter(FluxParticleDistanceFadeParameter.Target, Target)
                .Build();
    }
}
