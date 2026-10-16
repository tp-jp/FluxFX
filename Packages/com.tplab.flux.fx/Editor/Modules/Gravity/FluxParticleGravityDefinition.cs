using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Gravity
{
    /// <summary>
    /// GravityのParameterとVelocity更新Operationを提供します。
    /// </summary>
    public sealed class FluxParticleGravityDefinition : FluxParticleModuleDefinition<FluxParticleGravityModule>
    {
        public override string ModuleId => FluxParticleGravityContract.Module.Id;

        public override void Contribute(FluxParticleGravityModule gravity, FluxParticleProgramContribution context)
        {
            var contribution = new FluxParticleModuleContribution(context, FluxParticleGravityContract.Module, gravity.Enabled);
            var acceleration = contribution.Bind(FluxParticleGravityContract.Acceleration, gravity.Gravity);
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
