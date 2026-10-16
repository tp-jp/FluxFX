using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon.Modules;
namespace TpLab.Flux.FX.Editor.Modules.SizeOverLifetime
{
    /// <summary>
    /// Size over Lifetimeの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleSizeOverLifetimeContract
    {
        /// <summary>
        /// Linearモードの終端サイズの契約
        /// </summary>
        public static FluxParticleParameterContract<float> EndSize { get; } =
            FluxParticleParameter.Float("size.end")
                .ApiName("EndSize")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("tplab.fluxfx.size-over-lifetime")
                .Handle("FluxParticleSizeOverLifetimeHandle")
                .Accessor("SizeOverLifetime")
                .Enabled("size.enabled")
                .Parameter(FluxParticleSizeOverLifetimeParameter.EndSize, EndSize)
                .Build();
    }
}
