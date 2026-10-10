using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class LimitVelocityGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleLimitVelocityModule>
    {
        public override string ModuleId => "tplab.fluxfx.limit-velocity";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/LimitVelocity.hlsl";

        public override string EntryPoint => "FluxFX_LimitVelocity";

        public override int Order => 600;

        public override void CollectParameters(FluxParticleLimitVelocityModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat("_MaxSpeed", module.MaxSpeed);
        }
    }
}