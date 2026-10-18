namespace TpLab.Flux.FX.Editor.Contracts
{
    public static class FluxParticleRendererContract
    {
        /// <summary>
        /// 発光強度の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Intensity { get; } =
            FluxParticleParameter.Float("render.intensity").ApiName("Intensity")
                .Policy(FluxParticleParameterPolicy.Live).Minimum(0).Build();
    }
}
