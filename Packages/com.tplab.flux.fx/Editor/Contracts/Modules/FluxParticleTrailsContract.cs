using UnityEngine;

namespace TpLab.Flux.FX.Editor.Contracts.Modules
{
    /// <summary>
    /// Trailsの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleTrailsContract
    {
        /// <summary>
        /// 幅の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Width { get; } =
            FluxParticleParameter.Float("trails.width")
                .ApiName("Width")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// 色の契約
        /// </summary>
        public static FluxParticleParameterContract<Color> Color { get; } =
            FluxParticleParameter.Color("trails.color")
                .ApiName("Color")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 記録時間の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Duration { get; } =
            FluxParticleParameter.Float("trails.duration")
                .ApiName("Duration")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0, inclusive: false)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("tplab.fluxfx.trails")
                .Handle("FluxParticleTrailsHandle")
                .Accessor("Trails")
                .Enabled("trails.enabled")
                .Parameter(Width)
                .Parameter(Color)
                .Parameter(Duration)
                .Build();
    }
}
