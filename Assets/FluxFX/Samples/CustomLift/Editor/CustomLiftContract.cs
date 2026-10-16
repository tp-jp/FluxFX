using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules.Editor
{
    /// <summary>
    /// Custom Liftの公開APIとParameterの契約
    /// </summary>
    public static class CustomLiftContract
    {
        /// <summary>
        /// 加速度の強さの契約
        /// </summary>
        public static FluxParticleParameterContract<float> Strength { get; } =
            FluxParticleParameter.Float("custom-lift.strength")
                .ApiName("Strength")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 加速度の方向の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Direction { get; } =
            FluxParticleParameter.Vector3("custom-lift.direction")
                .ApiName("Direction")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(CustomLiftIds.Module)
                .Handle("CustomLiftHandle")
                .Accessor("CustomLift")
                .Enabled("custom-lift.enabled")
                .Parameter(CustomLiftParameter.Strength, Strength)
                .Parameter(CustomLiftParameter.Direction, Direction)
                .Build();
    }
}
