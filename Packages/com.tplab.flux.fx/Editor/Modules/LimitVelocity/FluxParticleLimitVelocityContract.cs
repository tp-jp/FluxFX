using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
namespace TpLab.Flux.FX.Editor.Modules.LimitVelocity
{
    /// <summary>
    /// LimitVelocityの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleLimitVelocityContract
    {
        /// <summary>
        /// 最大速度の契約
        /// </summary>
        public static FluxParticleParameterContract<float> MaxSpeed { get; } =
            FluxParticleParameter.Float("limit-velocity.max-speed")
                .ApiName("MaxSpeed")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.LimitVelocity)
                .Handle("FluxParticleLimitVelocityHandle")
                .Accessor("LimitVelocity")
                .Enabled("limit-velocity.enabled")
                .Parameter(FluxParticleLimitVelocityParameter.MaxSpeed, MaxSpeed)
                .Build();
    }
}
