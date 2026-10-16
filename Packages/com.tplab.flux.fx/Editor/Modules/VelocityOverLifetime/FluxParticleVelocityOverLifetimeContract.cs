using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Editor.Contracts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.VelocityOverLifetime
{
    /// <summary>
    /// VelocityOverLifetimeの公開APIとParameterの契約
    /// </summary>
    public static class FluxParticleVelocityOverLifetimeContract
    {
        /// <summary>
        /// 開始速度の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Start { get; } =
            FluxParticleParameter.Vector3("velocity-over-lifetime.start")
                .ApiName("Start")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 終端速度の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> End { get; } =
            FluxParticleParameter.Vector3("velocity-over-lifetime.end")
                .ApiName("End")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 周回速度の契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Orbital { get; } =
            FluxParticleParameter.Vector3("velocity-over-lifetime.orbital")
                .ApiName("Orbital")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 中心位置のオフセットの契約
        /// </summary>
        public static FluxParticleParameterContract<Vector3> Offset { get; } =
            FluxParticleParameter.Vector3("velocity-over-lifetime.offset")
                .ApiName("Offset")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// 放射方向の速度の契約
        /// </summary>
        public static FluxParticleParameterContract<float> Radial { get; } =
            FluxParticleParameter.Float("velocity-over-lifetime.radial")
                .ApiName("Radial")
                .Policy(FluxParticleParameterPolicy.Live)
                .Build();

        /// <summary>
        /// Moduleの契約
        /// </summary>
        public static FluxParticleModuleContract Module { get; } =
            new FluxParticleModuleContractBuilder(FluxParticleModuleIds.VelocityOverLifetime)
                .Handle("FluxParticleVelocityOverLifetimeHandle")
                .Accessor("VelocityOverLifetime")
                .Enabled("velocity-over-lifetime.enabled")
                .Parameter(FluxParticleVelocityOverLifetimeParameter.Start, Start)
                .Parameter(FluxParticleVelocityOverLifetimeParameter.End, End)
                .Parameter(FluxParticleVelocityOverLifetimeParameter.Orbital, Orbital)
                .Parameter(FluxParticleVelocityOverLifetimeParameter.Offset, Offset)
                .Parameter(FluxParticleVelocityOverLifetimeParameter.Radial, Radial)
                .Build();
    }
}
