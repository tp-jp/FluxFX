using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Vortex
{
    /// <summary>
    /// Step開始時のPositionから接線方向の加速度をVelocityへ加算します。
    /// </summary>
    public sealed class FluxParticleVortexDefinition : IFluxParticleModuleDefinition, IFluxParticleModuleBindingDefinition
    {
        public Type ModuleType => typeof(FluxParticleVortexModule);
        public string ModuleId => "tplab.fluxfx.vortex";
        public string EnabledParameterId => "vortex.enabled";
        public FluxParticleParameterDefinition[] GetHandleParameters(FluxParticleModule module) => new FluxParticleParameterDefinition[]
        {
            new FluxParticleParameterDefinition("vortex.center", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live),
            new FluxParticleParameterDefinition("vortex.axis", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live),
            new FluxParticleParameterDefinition("vortex.strength", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live)
        };

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var handleParameters = GetHandleParameters(module);
            var vortex = (FluxParticleVortexModule)module;
            if (vortex.Strength < 0) throw new InvalidOperationException("Vortex strength must be non-negative.");
            context.AddParameter(handleParameters[0], vortex.Center);
            context.AddParameter(handleParameters[1], vortex.Axis);
            context.AddParameter(handleParameters[2], new Vector4(vortex.Strength, 0, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition("vortex.enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("vortex", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Modules/Vortex/Vortex.hlsl", "FluxFX_Vortex",
                new[] { "vortex.center", "vortex.axis", "vortex.strength" }, "vortex.enabled"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("vortex", "gravity"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("vortex", "force"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("vortex", "noise"));
        }
    }
}
