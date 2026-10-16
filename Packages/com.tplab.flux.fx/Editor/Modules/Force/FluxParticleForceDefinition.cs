using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Force
{
    /// <summary>
    /// 一定の加速度をVelocityへ加算するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleForceDefinition : FluxParticleModuleDefinition<FluxParticleForceModule>
    {

        public override string ModuleId => FluxParticleForceContract.Module.Id;

        public override void Contribute(FluxParticleForceModule force, FluxParticleProgramContribution context)
        {
            var contribution = new FluxParticleModuleContribution(context, FluxParticleForceContract.Module, force.Enabled);
            var forceParameter = contribution.Bind(FluxParticleForceContract.Force, force.Force);
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
