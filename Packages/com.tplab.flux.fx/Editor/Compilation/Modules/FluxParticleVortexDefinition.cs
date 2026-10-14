using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Step開始時のPositionから接線方向の加速度をVelocityへ加算します。
    /// </summary>
    public sealed class FluxParticleVortexDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleVortexModule);
        public string ModuleId => "tplab.fluxfx.vortex";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var vortex = (FluxParticleVortexModule)module;
            if (vortex.Strength < 0) throw new InvalidOperationException("Vortex strength must be non-negative.");
            context.AddParameter(new FluxParticleParameterDefinition("vortex.center", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), vortex.Center);
            context.AddParameter(new FluxParticleParameterDefinition("vortex.axis", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), vortex.Axis);
            context.AddParameter(new FluxParticleParameterDefinition("vortex.strength", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(vortex.Strength, 0, 0, 0));
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
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Vortex.hlsl", "FluxFX_Vortex",
                new[] { "vortex.center", "vortex.axis", "vortex.strength" }, "vortex.enabled"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("vortex", "gravity"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("vortex", "force"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("vortex", "noise"));
        }
    }
}
