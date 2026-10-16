using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Force
{
    /// <summary>
    /// Forceの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleForceContract
    {
        /// <summary>
        /// 加速度の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Force { get; } =
            FluxParticleParameter.Vector3("force")
                .ApiName("Force")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.Force)
                .Handle("FluxParticleForceHandle")
                .Accessor("Force")
                .Enabled("force.enabled")
                .Parameter(FluxParticleForceParameter.Force, Force)
                .Build();
    }
}
