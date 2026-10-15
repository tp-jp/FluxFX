using UnityEngine;

namespace TpLab.Flux.FX.Editor.Contracts.Modules
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
            new FluxParticleModuleContractBuilder("tplab.fluxfx.rotation-over-lifetime")
                .Handle("FluxParticleRotationOverLifetimeHandle")
                .Accessor("RotationOverLifetime")
                .Enabled("rotation.enabled")
                .Parameter(EndRotation)
                .Build();
    }
}
