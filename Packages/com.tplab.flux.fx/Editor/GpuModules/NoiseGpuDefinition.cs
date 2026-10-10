using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class NoiseGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleNoiseModule>
    {
        public override string ModuleId => "tplab.fluxfx.noise";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Noise.hlsl";

        public override string EntryPoint => "FluxFX_Noise";

        public override int Order => 300;

        public override void CollectParameters(FluxParticleNoiseModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat("_NoiseStrength", module.Strength);
            collector.AddFloat("_NoiseScale", module.Scale);
            collector.AddFloat("_NoiseSpeed", module.Speed);
        }
    }
}