using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class DragGpuDefinition : FluxParticleGpuModuleDefinition
    {
        public override Type ModuleType => typeof(FluxParticleDragModule);

        public override string ModuleId => "tplab.fluxfx.drag";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/Drag.hlsl";

        public override string EntryPoint => "FluxFX_Drag";

        public override int Order => 500;
    }
}