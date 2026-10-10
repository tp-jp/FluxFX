using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class GravityGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleGravityModule>
    {
        public override string ModuleId => "tplab.fluxfx.gravity";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Gravity.hlsl";

        public override string EntryPoint => "FluxFX_Gravity";

        public override int Order => 100;
    }
}