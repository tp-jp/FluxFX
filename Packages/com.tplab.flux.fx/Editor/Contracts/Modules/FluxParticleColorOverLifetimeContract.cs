using UnityEngine;

namespace TpLab.Flux.FX.Editor.Contracts.Modules
{
    /// <summary>
    /// ColorOverLifetimeの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleColorOverLifetimeContract
    {
        /// <summary>
        /// 終端色の契約
        /// </summary>
        public static FluxParticleParameterContract<Color> EndColor { get; } =
            FluxParticleParameter.Color("color.end")
                .ApiName("EndColor")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("fluxfx.color-over-lifetime")
                .Handle("FluxParticleColorOverLifetimeHandle")
                .Accessor("ColorOverLifetime")
                .Enabled("color.enabled")
                .Parameter(EndColor)
                .Build();
    }
}
