using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class DragGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleDragModule>
    {
        public override string ModuleId => "tplab.fluxfx.drag";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Drag.hlsl";

        public override string EntryPoint => "FluxFX_Drag";

        public override int Order => 500;

        public override void CollectParameters(FluxParticleDragModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat("_Drag", module.Drag);
        }
    }
}