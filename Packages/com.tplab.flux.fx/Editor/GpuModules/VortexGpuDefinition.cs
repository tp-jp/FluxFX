using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class VortexGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleVortexModule>
    {
        public override string ModuleId => "tplab.fluxfx.vortex";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Vortex.hlsl";

        public override string EntryPoint => "FluxFX_Vortex";

        public override int Order => 400;

        public override void CollectParameters(FluxParticleVortexModule module, FluxParticleParameterCollector collector)
        {
            collector.AddVector("_VortexCenter", module.Center);
            collector.AddVector("_VortexAxis", module.Axis.normalized);
            collector.AddFloat("_VortexStrength", module.Strength);
        }
    }
}