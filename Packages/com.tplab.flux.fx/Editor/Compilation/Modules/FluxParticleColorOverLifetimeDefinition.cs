using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Color over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleColorOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleColorOverLifetimeModule);
        public string ModuleId => "fluxfx.color-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var color = (FluxParticleColorOverLifetimeModule)module;
            if (color.Mode != FluxParticleColorOverLifetimeMode.Linear)
            {
                throw new InvalidOperationException("Program Backend currently requires: Linear color over lifetime.");
            }

            context.SetInitialValue("color.end", color.EndColor);
            context.AddOperation(new FluxParticleOperationDefinition("render.color", ModuleId, FluxParticlePhase.Render,
                new[]
                {
                    new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                new[] { "render.start" },
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/RenderColor.hlsl", "FluxFX_RenderColor",
                new[] { "color.end" }));
        }
    }
}