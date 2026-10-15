using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.Gravity
{
    /// <summary>
    /// GravityのParameterとVelocity更新Operationを提供します。
    /// </summary>
    public sealed class FluxParticleGravityDefinition : IFluxParticleModuleDefinition
    {
        const int GravitySlot = 0;
        public Type ModuleType => typeof(FluxParticleGravityModule);
        public string ModuleId => "fluxfx.gravity";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var gravity = (FluxParticleGravityModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "gravity.enabled", gravity.Enabled, 1);
            var acceleration = contribution.Parameter(GravitySlot, "gravity", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, gravity.Gravity);
            contribution.Operation("gravity", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Gravity/Gravity.hlsl",
                entryPoint: "FluxFX_Gravity", switchable: true, parameters: new[] { acceleration });
            contribution.Complete();
        }
    }
}