using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts.Modules;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.ColorOverLifetime
{
    /// <summary>
    /// Color over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleColorOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/ColorOverLifetime/";

        public Type ModuleType => typeof(FluxParticleColorOverLifetimeModule);

        public string ModuleId => FluxParticleColorOverLifetimeContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var color = (FluxParticleColorOverLifetimeModule)module;
            if (color.Mode != FluxParticleColorOverLifetimeMode.Linear && color.Mode != FluxParticleColorOverLifetimeMode.Gradient)
            {
                throw new InvalidOperationException("Unsupported color over lifetime mode.");
            }

            var contribution = new FluxParticleModuleContribution(context, FluxParticleColorOverLifetimeContract.Module, color.Enabled);
            var start = new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start");
            FluxParticleValueRead[] reads;
            string hlsl;
            string entryPoint;
            var parameters = Array.Empty<string>();
            if (color.Mode == FluxParticleColorOverLifetimeMode.Linear)
            {
                var end = contribution.Bind(FluxParticleColorOverLifetimeContract.EndColor, color.EndColor);
                parameters = new[] { end };
                reads = new[]
                {
                    start,
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
                };
                hlsl = "RenderColor.hlsl";
                entryPoint = "FluxFX_RenderColor";
            }
            else
            {
                contribution.UnavailableParameter(FluxParticleColorOverLifetimeContract.EndColor);
                var age = contribution.NormalizedAge("color.normalized-age");
                var gradient = contribution.Gradient("color.gradient", age, color.Gradient);
                reads = new[] { start, new FluxParticleValueRead(gradient, FluxParticleReadVersion.Provided) };
                hlsl = "RenderColorGradient.hlsl";
                entryPoint = "FluxFX_RenderColorGradient";
            }
            contribution.Operation("render.color", FluxParticlePhase.Render,
                reads: reads,
                writes: new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                hlslPath: Hlsl + hlsl, entryPoint: entryPoint, switchable: true, parameters: parameters);
            contribution.Complete();
        }
    }
}
