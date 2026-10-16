using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
namespace TpLab.Flux.FX.Editor.Modules.Drag
{
    /// <summary>
    /// Dragの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleDragContract
    {
        /// <summary>
        /// 抵抗の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Drag { get; } =
            FluxParticleParameter.Float("drag")
                .ApiName("Drag")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.Drag)
                .Handle("FluxParticleDragHandle")
                .Accessor("Drag")
                .Enabled("drag.enabled")
                .Parameter(FluxParticleDragParameter.Drag, Drag)
                .Build();
    }
}
