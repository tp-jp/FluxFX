using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.LimitVelocity
{
    /// <summary>
    /// Velocityの大きさを指定した上限へ制限するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleLimitVelocityDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleLimitVelocityModule);

        public string ModuleId => FluxParticleLimitVelocityContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var limit = (FluxParticleLimitVelocityModule)module;
            var contribution = new FluxParticleModuleContribution(context, FluxParticleLimitVelocityContract.Module, limit.Enabled);
            var maxSpeedParameter = contribution.Bind(FluxParticleLimitVelocityContract.MaxSpeed, limit.MaxSpeed);
            contribution.Operation("limit-velocity", FluxParticlePhase.Update,
                reads: new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/LimitVelocity/LimitVelocity.hlsl", entryPoint: "FluxFX_LimitVelocity",
                switchable: true, parameters: new[] { maxSpeedParameter },
                optionalDependencies: new[] { "gravity", "force", "noise", "vortex", "drag" });
            contribution.Complete();
        }
    }
}
