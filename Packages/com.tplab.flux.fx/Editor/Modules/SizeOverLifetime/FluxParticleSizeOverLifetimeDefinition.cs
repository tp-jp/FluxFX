using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.SizeOverLifetime
{
    /// <summary>
    /// Size over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleSizeOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/SizeOverLifetime/";
        const int EndSizeSlot = 0;
        public Type ModuleType => typeof(FluxParticleSizeOverLifetimeModule);
        public string ModuleId => "tplab.fluxfx.size-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var size = (FluxParticleSizeOverLifetimeModule)module;
            if (size.Mode != FluxParticleSizeOverLifetimeMode.Linear && size.Mode != FluxParticleSizeOverLifetimeMode.Curve)
            {
                throw new InvalidOperationException("Unsupported size over lifetime mode.");
            }

            var contribution = new FluxParticleModuleContribution(context, ModuleId, "size.enabled", size.Enabled, 1);
            var start = new FluxParticleValueRead("render.size", FluxParticleReadVersion.AfterOperation, "render.size-start");
            FluxParticleValueRead[] reads;
            string hlsl;
            var parameters = Array.Empty<string>();
            if (size.Mode == FluxParticleSizeOverLifetimeMode.Linear)
            {
                if (float.IsNaN(size.EndSize) || float.IsInfinity(size.EndSize) || size.EndSize < 0)
                {
                    throw new InvalidOperationException("End size must be finite and non-negative.");
                }
                var end = contribution.Parameter(EndSizeSlot, "size.end", FluxParticleValueType.Float,
                    FluxParticleParameterPolicy.Live, new Vector4(size.EndSize, 0, 0, 0));
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
                contribution.UnavailableParameter(EndSizeSlot);
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
