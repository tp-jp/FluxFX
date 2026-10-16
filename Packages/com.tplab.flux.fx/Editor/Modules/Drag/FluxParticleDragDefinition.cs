using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Drag
{
    /// <summary>
    /// DragのParameterとVelocity更新Operationを提供します。
    /// </summary>
    public sealed class FluxParticleDragDefinition : FluxParticleModuleDefinition<FluxParticleDragModule>
    {

        public override string ModuleId => FluxParticleDragContract.Module.Id;

        public override void Contribute(FluxParticleDragModule drag, FluxParticleProgramContribution context)
        {
            var contribution = new FluxParticleModuleContribution(context, FluxParticleDragContract.Module, drag.Enabled);
            var dragParameter = contribution.Bind(FluxParticleDragContract.Drag, drag.Drag);
            contribution.Operation("drag", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Drag/Drag.hlsl", entryPoint: "FluxFX_Drag",
                switchable: true, parameters: new[] { dragParameter },
                optionalDependencies: new[] { "gravity", "force", "vortex", "noise" });
            contribution.Complete();
        }
    }
}
