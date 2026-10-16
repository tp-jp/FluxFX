using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.VelocityOverLifetime
{
    /// <summary>
    /// 時間変化・周回・放射方向の速度を位置へ加算するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleVelocityOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/VelocityOverLifetime/";
        const string Prefix = "velocity-over-lifetime.";

        public Type ModuleType => typeof(FluxParticleVelocityOverLifetimeModule);

        public string ModuleId => FluxParticleVelocityOverLifetimeContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var velocity = (FluxParticleVelocityOverLifetimeModule)module;
            var contribution = new FluxParticleModuleContribution(context, FluxParticleVelocityOverLifetimeContract.Module, velocity.Enabled);
            var startParameter = contribution.Bind(FluxParticleVelocityOverLifetimeContract.Start, velocity.Start);
            var endParameter = contribution.Bind(FluxParticleVelocityOverLifetimeContract.End, velocity.End);
            var orbitalParameter = contribution.Bind(FluxParticleVelocityOverLifetimeContract.Orbital, velocity.Orbital);
            var offsetParameter = contribution.Bind(FluxParticleVelocityOverLifetimeContract.Offset, velocity.Offset);
            var radialParameter = contribution.Bind(FluxParticleVelocityOverLifetimeContract.Radial, velocity.Radial);
            context.AddValue(new FluxParticleValueDefinition(Prefix + "center-offset", FluxParticleValueType.Float3,
                FluxParticleWritePolicy.ReadOnly, ModuleId));
            context.AddProvider(new FluxParticleValueProvider(Prefix + "center-offset", FluxParticleProviderKind.Derived,
                new[] { "particle.position" }, new[] { offsetParameter }, Hlsl + "VelocityOverLifetimeOffset.hlsl",
                "FluxFX_VelocityOverLifetimeOffset"));
            contribution.Operation("velocity-over-lifetime", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.AfterOperation, "motion"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead(Prefix + "center-offset", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.position", FluxParticleWriteKind.Modify) },
                hlslPath: Hlsl + "VelocityOverLifetime.hlsl", entryPoint: "FluxFX_VelocityOverLifetime",
                switchable: true, parameters: new[] { startParameter, endParameter, orbitalParameter, radialParameter });
            contribution.Complete();
        }
    }
}
