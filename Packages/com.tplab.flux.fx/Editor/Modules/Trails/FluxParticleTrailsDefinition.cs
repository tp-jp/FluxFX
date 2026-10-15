using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts.Modules;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.Trails
{
    public sealed class FluxParticleTrailsDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleTrailsModule);

        public string ModuleId => FluxParticleTrailsContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var trails = (FluxParticleTrailsModule)module;
            if (trails.MaxTrails < 1 || trails.MaxTrails > FluxParticleCapacity.MaximumExactCount ||
                trails.PointsPerTrail < 2 || trails.PointsPerTrail >= FluxParticleCapacity.MaximumExactCount)
            {
                throw new InvalidOperationException("Trail capacity must be positive and point count must be at least two, within float integer precision.");
            }
            if (!FluxParticleTrailHistory.IsValidDuration(trails.Duration, trails.PointsPerTrail))
            {
                throw new InvalidOperationException("Trail duration and sample interval must be finite and positive.");
            }
            var contribution = new FluxParticleModuleContribution(context, FluxParticleTrailsContract.Module, trails.Enabled);
            contribution.Bind(FluxParticleTrailsContract.Width, trails.Width);
            contribution.Bind(FluxParticleTrailsContract.Color, trails.Color);
            contribution.Bind(FluxParticleTrailsContract.Duration, trails.Duration);
            contribution.Complete();
        }
    }
}
