using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.LimitVelocity
{
    /// <summary>
    /// Velocityの大きさを指定した上限へ制限するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleLimitVelocityDefinition : IFluxParticleModuleDefinition
    {
        const int MaxSpeedSlot = 0;

        public Type ModuleType => typeof(FluxParticleLimitVelocityModule);

        public string ModuleId => "tplab.fluxfx.limit-velocity";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var limit = (FluxParticleLimitVelocityModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "limit-velocity.enabled", limit.Enabled, 1);
            if (float.IsNaN(limit.MaxSpeed) || float.IsInfinity(limit.MaxSpeed) || limit.MaxSpeed < 0)
            {
                throw new InvalidOperationException("Maximum speed must be finite and non-negative.");
            }
            var maxSpeedParameter = contribution.Parameter(MaxSpeedSlot, "limit-velocity.max-speed", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(limit.MaxSpeed, 0, 0, 0));
            contribution.Operation("limit-velocity", FluxParticlePhase.Update,
                reads: new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/LimitVelocity/LimitVelocity.hlsl", entryPoint: "FluxFX_LimitVelocity",
                switchable: true, parameters: new[] { maxSpeedParameter },
                optionalDependencies: new[] { "gravity", "force", "noise", "vortex", "drag" });
            contribution.Complete();
        }
    }
}
