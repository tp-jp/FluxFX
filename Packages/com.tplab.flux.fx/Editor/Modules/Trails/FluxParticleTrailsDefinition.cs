using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Trails
{
    public sealed class FluxParticleTrailsDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleTrailsModule);

        public string ModuleId => "tplab.fluxfx.trails";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var trails = (FluxParticleTrailsModule)module;
            if (float.IsNaN(trails.Width) || float.IsInfinity(trails.Width) || trails.Width < 0)
            {
                throw new InvalidOperationException("Trail width must be finite and non-negative.");
            }
            if (trails.MaxTrails < 1 || trails.MaxTrails > FluxParticleCapacity.MaximumExactCount ||
                trails.PointsPerTrail < 2 || trails.PointsPerTrail >= FluxParticleCapacity.MaximumExactCount)
            {
                throw new InvalidOperationException("Trail capacity must be positive and point count must be at least two, within float integer precision.");
            }
            if (!FluxParticleTrailHistory.IsValidDuration(trails.Duration, trails.PointsPerTrail))
            {
                throw new InvalidOperationException("Trail duration and sample interval must be finite and positive.");
            }
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "trails.enabled", trails.Enabled, 3);
            contribution.Parameter(0, "trails.width", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live, new Vector4(trails.Width, 0, 0, 0));
            contribution.Parameter(1, "trails.color", FluxParticleValueType.Float4, FluxParticleParameterPolicy.Live, trails.Color);
            contribution.Parameter(2, "trails.duration", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live, new Vector4(trails.Duration, 0, 0, 0));
            contribution.Complete();
        }
    }
}
