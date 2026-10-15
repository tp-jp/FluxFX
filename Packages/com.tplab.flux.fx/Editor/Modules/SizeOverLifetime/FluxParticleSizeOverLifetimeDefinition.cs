using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts.Modules;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.SizeOverLifetime
{
    /// <summary>
    /// Size over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleSizeOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/SizeOverLifetime/";
        public Type ModuleType => typeof(FluxParticleSizeOverLifetimeModule);
        public string ModuleId => FluxParticleSizeOverLifetimeContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var size = (FluxParticleSizeOverLifetimeModule)module;
            if (size.Mode != FluxParticleSizeOverLifetimeMode.Linear && size.Mode != FluxParticleSizeOverLifetimeMode.Curve)
            {
                throw new InvalidOperationException("Unsupported size over lifetime mode.");
            }

            var contribution = new FluxParticleModuleContribution(context, FluxParticleSizeOverLifetimeContract.Module, size.Enabled);
            var start = new FluxParticleValueRead("render.size", FluxParticleReadVersion.AfterOperation, "render.size-start");
            FluxParticleValueRead[] reads;
            string hlsl;
            var parameters = Array.Empty<string>();
            if (size.Mode == FluxParticleSizeOverLifetimeMode.Linear)
            {
                var end = contribution.Bind(FluxParticleSizeOverLifetimeContract.EndSize, size.EndSize);
                parameters = new[] { end };
                reads = new[]
                {
                    start,
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
                };
                hlsl = "SizeOverLifetimeLinear.hlsl";
            }
            else
            {
                contribution.UnavailableParameter(FluxParticleSizeOverLifetimeContract.EndSize);
                var age = contribution.NormalizedAge("size.normalized-age");
                var curve = contribution.Curve("size.curve", age, size.Curve);
                reads = new[] { start, new FluxParticleValueRead(curve, FluxParticleReadVersion.Provided) };
                hlsl = "SizeOverLifetimeCurve.hlsl";
            }
            contribution.Operation("render.size-over-lifetime", FluxParticlePhase.Render,
                reads: reads,
                writes: new[] { new FluxParticleValueWrite("render.size", FluxParticleWriteKind.Modify) },
                hlslPath: Hlsl + hlsl, entryPoint: "FluxFX_SizeOverLifetime",
                switchable: true, parameters: parameters);
            contribution.Complete();
        }
    }
}
