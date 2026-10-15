using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Gravity
{
    /// <summary>
    /// GravityのParameterとVelocity更新Operationを提供します。
    /// </summary>
    public sealed class FluxParticleGravityDefinition : IFluxParticleModuleDefinition, IFluxParticleModuleBindingDefinition
    {
        public Type ModuleType => typeof(FluxParticleGravityModule);
        public string ModuleId => "fluxfx.gravity";
        public string EnabledParameterId => "gravity.enabled";
        public FluxParticleParameterDefinition[] GetHandleParameters(FluxParticleModule module) => new[]
        {
            new FluxParticleParameterDefinition("gravity", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live)
        };

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var handleParameters = GetHandleParameters(module);
            var gravity = (FluxParticleGravityModule)module;
            context.AddParameter(handleParameters[0], gravity.Gravity);
            context.AddParameter(new FluxParticleParameterDefinition("gravity.enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("gravity", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Modules/Gravity/Gravity.hlsl", "FluxFX_Gravity",
                new[] { "gravity" }, "gravity.enabled"));
        }
    }
}
