using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Trails
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
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.Trails)
                .Handle("FluxParticleTrailsHandle")
                .Accessor("Trails")
                .Enabled("trails.enabled")
                .Parameter(FluxParticleTrailsParameter.Width, Width)
                .Parameter(FluxParticleTrailsParameter.Color, Color)
                .Parameter(FluxParticleTrailsParameter.Duration, Duration)
                .Build();
    }
}
