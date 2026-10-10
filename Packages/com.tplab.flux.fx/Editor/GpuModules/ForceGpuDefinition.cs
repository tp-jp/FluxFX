using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class ForceGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleForceModule>
    {
        public override string ModuleId => "tplab.fluxfx.force";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Force.hlsl";

        public override string EntryPoint => "FluxFX_Force";

        public override int Order => 200;

        public override void CollectParameters(FluxParticleForceModule module, FluxParticleParameterCollector collector)
        {
            collector.AddVector("_Force", module.Force);
        }
    }
}