using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
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
            context.SetInitialValue("drag", new Vector4(drag.Drag, 0, 0, 0));
            context.SetInitialValue("drag.enabled", new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("drag", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Drag.hlsl", "FluxFX_Drag",
                new[] { "drag" }, "drag.enabled", velocityOrder: 500));
        }
    }
}