using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public sealed class VelocityOverLifetimeGpuDefinition : FluxParticleGpuModuleDefinition<FluxParticleVelocityOverLifetimeModule>
    {
        public override string ModuleId => "tplab.fluxfx.velocity-over-lifetime";

        public override string HlslPath => "Packages/com.tplab.flux.fx/Runtime/Shaders/Modules/VelocityOverLifetime.hlsl";

        public override string EntryPoint => "FluxFX_VelocityOverLifetime";

        public override int Order => 100;

        public override void CollectParameters(FluxParticleVelocityOverLifetimeModule module, FluxParticleParameterCollector collector)
        {
            collector.AddVector("_VelocityOverLifetimeStart", new Vector4(module.Start.x, module.Start.y, module.Start.z, 0));
            collector.AddVector("_VelocityOverLifetimeEnd", new Vector4(module.End.x, module.End.y, module.End.z, 0));
            collector.AddVector("_VelocityOverLifetimeOrbital", new Vector4(module.Orbital.x, module.Orbital.y, module.Orbital.z, 0));
            collector.AddVector("_VelocityOverLifetimeOffset", new Vector4(module.Offset.x, module.Offset.y, module.Offset.z, 0));
            collector.AddFloat("_VelocityOverLifetimeRadial", module.Radial);
        }
    }
}