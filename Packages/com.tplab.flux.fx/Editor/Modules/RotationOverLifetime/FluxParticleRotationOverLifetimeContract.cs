using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.RotationOverLifetime
{
    /// <summary>
    /// RotationOverLifetimeの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleRotationOverLifetimeContract
    {
        /// <summary>
        /// 終端回転の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> EndRotation { get; } =
            FluxParticleParameter.Vector3("rotation.end")
                .ApiName("EndRotation")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.RotationOverLifetime)
                .Handle("FluxParticleRotationOverLifetimeHandle")
                .Accessor("RotationOverLifetime")
                .Enabled("rotation.enabled")
                .Parameter(FluxParticleRotationOverLifetimeParameter.EndRotation, EndRotation)
                .Build();
    }
}
