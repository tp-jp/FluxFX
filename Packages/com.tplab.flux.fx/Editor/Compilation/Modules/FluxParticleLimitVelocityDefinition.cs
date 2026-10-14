using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Velocityの大きさを指定した上限へ制限するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleLimitVelocityDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleLimitVelocityModule);
        public string ModuleId => "tplab.fluxfx.limit-velocity";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var limit = (FluxParticleLimitVelocityModule)module;
            if (float.IsNaN(limit.MaxSpeed) || float.IsInfinity(limit.MaxSpeed) || limit.MaxSpeed < 0)
            {
                throw new InvalidOperationException("Maximum speed must be finite and non-negative.");
            }
            context.AddParameter(new FluxParticleParameterDefinition("limit-velocity.max-speed", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(limit.MaxSpeed, 0, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition("limit-velocity.enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("limit-velocity", ModuleId, FluxParticlePhase.Update,
                new[] { new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious) },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/LimitVelocity.hlsl", "FluxFX_LimitVelocity",
                new[] { "limit-velocity.max-speed" }, "limit-velocity.enabled"));
            foreach (var operation in new[] { "gravity", "force", "drag" })
            {
                context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("limit-velocity", operation));
            }
        }
    }
}
