using System;
using TpLab.Flux.FX.Editor.GpuModules;

namespace TpLab.Flux.FX.CustomModules.Editor
{
    public sealed class CustomLiftGpuDefinition : FluxParticleGpuModuleDefinition
    {
        public override Type ModuleType => typeof(CustomLiftModule);

        public override string ModuleId => "custom.fluxfx.lift";

        public override string HlslPath => "Assets/FluxFX/CustomModules/CustomLift.hlsl";

        public override string EntryPoint => "FluxFX_CustomLift";

        public override int Order => 150;
    }
}