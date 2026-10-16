using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Gravity
{
    /// <summary>
    /// Gravityの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleGravityContract
    {
        /// <summary>
        /// 重力加速度の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Acceleration { get; } =
            FluxParticleParameter.Vector3("gravity")
                .ApiName("Gravity")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder("fluxfx.gravity")
                .Handle("FluxParticleGravityHandle")
                .Accessor("Gravity")
                .Enabled("gravity.enabled")
                .Parameter(FluxParticleGravityParameter.Gravity, Acceleration)
                .Build();
    }
}
