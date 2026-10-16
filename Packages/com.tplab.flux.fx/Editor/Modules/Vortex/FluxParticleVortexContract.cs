using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Vortex
{
    /// <summary>
    /// Vortexの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleVortexContract
    {
        /// <summary>
        /// 中心位置の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Center { get; } =
            FluxParticleParameter.Vector3("vortex.center")
                .ApiName("Center")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 回転軸の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Axis { get; } =
            FluxParticleParameter.Vector3("vortex.axis")
                .ApiName("Axis")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 強さの契約
        /// </summary>
        public static FluxParticleParameterContract<float> Strength { get; } =
            FluxParticleParameter.Float("vortex.strength")
                .ApiName("Strength")
                .Policy(FluxParticleParameterPolicy.Live)
                .Minimum(0)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("tplab.fluxfx.vortex")
                .Handle("FluxParticleVortexHandle")
                .Accessor("Vortex")
                .Enabled("vortex.enabled")
                .Parameter(FluxParticleVortexParameter.Center, Center)
                .Parameter(FluxParticleVortexParameter.Axis, Axis)
                .Parameter(FluxParticleVortexParameter.Strength, Strength)
                .Build();
    }
}
