using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.Force
{
    /// <summary>
    /// 一定の加速度をVelocityへ加算するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleForceDefinition : IFluxParticleModuleDefinition
    {
        const int ForceSlot = 0;

        public Type ModuleType => typeof(FluxParticleForceModule);

        public string ModuleId => "tplab.fluxfx.force";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var force = (FluxParticleForceModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "force.enabled", force.Enabled, 1);
            var forceParameter = contribution.Parameter(ForceSlot, "force", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, force.Force);
            contribution.Operation("force", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Force/Force.hlsl", entryPoint: "FluxFX_Force",
                switchable: true, parameters: new[] { forceParameter },
                optionalDependencies: new[] { "gravity" });
            contribution.Complete();
        }
    }
}
