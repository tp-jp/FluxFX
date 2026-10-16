using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.ColorOverLifetime
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
                .Parameter(FluxParticleColorOverLifetimeParameter.EndColor, EndColor)
                .Build();
    }
}
