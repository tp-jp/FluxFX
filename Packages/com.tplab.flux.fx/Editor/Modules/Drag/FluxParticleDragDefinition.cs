using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Drag
{
    /// <summary>
    /// DragのParameterとVelocity更新Operationを提供します。
    /// </summary>
    public sealed class FluxParticleDragDefinition : IFluxParticleModuleDefinition
    {
        const int DragSlot = 0;

        public Type ModuleType => typeof(FluxParticleDragModule);

        public string ModuleId => "fluxfx.drag";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var drag = (FluxParticleDragModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "drag.enabled", drag.Enabled, 1);
            var dragParameter = contribution.Parameter(DragSlot, "drag", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(drag.Drag, 0, 0, 0));
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
