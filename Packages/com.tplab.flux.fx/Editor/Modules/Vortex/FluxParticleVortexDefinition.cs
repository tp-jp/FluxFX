using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Vortex
{
    /// <summary>
    /// Step開始時のPositionから接線方向の加速度をVelocityへ加算します。
    /// </summary>
    public sealed class FluxParticleVortexDefinition : IFluxParticleModuleDefinition
    {
        const int CenterSlot = 0;
        const int AxisSlot = 1;
        const int StrengthSlot = 2;

        public Type ModuleType => typeof(FluxParticleVortexModule);

        public string ModuleId => "tplab.fluxfx.vortex";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var vortex = (FluxParticleVortexModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "vortex.enabled", vortex.Enabled, 3);
            if (vortex.Strength < 0) throw new InvalidOperationException("Vortex strength must be non-negative.");
            var centerParameter = contribution.Parameter(CenterSlot, "vortex.center", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, vortex.Center);
            var axisParameter = contribution.Parameter(AxisSlot, "vortex.axis", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live, vortex.Axis);
            var strengthParameter = contribution.Parameter(StrengthSlot, "vortex.strength", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(vortex.Strength, 0, 0, 0));
            contribution.Operation("vortex", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Vortex/Vortex.hlsl", entryPoint: "FluxFX_Vortex",
                switchable: true, parameters: new[] { centerParameter, axisParameter, strengthParameter },
                optionalDependencies: new[] { "gravity", "force", "noise" });
            contribution.Complete();
        }
    }
}
