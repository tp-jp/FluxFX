using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.VelocityOverLifetime
{
    /// <summary>
    /// 時間変化・周回・放射方向の速度を位置へ加算するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleVelocityOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/VelocityOverLifetime/";
        const string Prefix = "velocity-over-lifetime.";
        const int StartSlot = 0;
        const int EndSlot = 1;
        const int OrbitalSlot = 2;
        const int OffsetSlot = 3;
        const int RadialSlot = 4;

        public Type ModuleType => typeof(FluxParticleVelocityOverLifetimeModule);

        public string ModuleId => "tplab.fluxfx.velocity-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var velocity = (FluxParticleVelocityOverLifetimeModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "velocity-over-lifetime.enabled", velocity.Enabled, 5);
            var startParameter = contribution.Parameter(StartSlot, "velocity-over-lifetime.start", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, velocity.Start);
            var endParameter = contribution.Parameter(EndSlot, "velocity-over-lifetime.end", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, velocity.End);
            var orbitalParameter = contribution.Parameter(OrbitalSlot, "velocity-over-lifetime.orbital", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, velocity.Orbital);
            var offsetParameter = contribution.Parameter(OffsetSlot, "velocity-over-lifetime.offset", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, velocity.Offset);
            var radialParameter = contribution.Parameter(RadialSlot, "velocity-over-lifetime.radial", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(velocity.Radial, 0, 0, 0));
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
