using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
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
        public Type ModuleType => typeof(FluxParticleDragModule);
        public string ModuleId => "fluxfx.drag";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var drag = (FluxParticleDragModule)module;
            context.AddParameter(new FluxParticleParameterDefinition("drag", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(drag.Drag, 0, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition("drag.enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("drag", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Modules/Drag/Drag.hlsl", "FluxFX_Drag",
                new[] { "drag" }, "drag.enabled"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("drag", "gravity"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("drag", "force"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("drag", "vortex"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("drag", "noise"));
        }
    }
}