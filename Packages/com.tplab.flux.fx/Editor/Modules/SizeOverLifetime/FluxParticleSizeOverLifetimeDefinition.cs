using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Size over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleSizeOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/SizeOverLifetime/";
        public Type ModuleType => typeof(FluxParticleSizeOverLifetimeModule);
        public string ModuleId => "tplab.fluxfx.size-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var size = (FluxParticleSizeOverLifetimeModule)module;
            if (size.Mode != FluxParticleSizeOverLifetimeMode.Linear && size.Mode != FluxParticleSizeOverLifetimeMode.Curve)
            {
                throw new InvalidOperationException("Unsupported size over lifetime mode.");
            }
            var reads = new[]
            {
                new FluxParticleValueRead("render.size", FluxParticleReadVersion.AfterOperation, "render.size-start"),
                new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
            };
            var hlsl = "SizeOverLifetimeLinear.hlsl";
            var parameters = Array.Empty<string>();
            if (size.Mode == FluxParticleSizeOverLifetimeMode.Linear)
            {
                if (float.IsNaN(size.EndSize) || float.IsInfinity(size.EndSize) || size.EndSize < 0)
                {
                    throw new InvalidOperationException("End size must be finite and non-negative.");
                }
                context.AddParameter(new FluxParticleParameterDefinition("size.end", FluxParticleValueType.Float,
                    FluxParticleParameterPolicy.Live), new Vector4(size.EndSize, 0, 0, 0));
                parameters = new[] { "size.end" };
            }
            else
            {
                context.AddValue(new FluxParticleValueDefinition("size.normalized-age", FluxParticleValueType.Float,
                    FluxParticleWritePolicy.ReadOnly, ModuleId));
                context.AddValue(new FluxParticleValueDefinition("size.curve", FluxParticleValueType.Float,
                    FluxParticleWritePolicy.ReadOnly, ModuleId));
                context.AddProvider(new FluxParticleValueProvider("size.normalized-age", FluxParticleProviderKind.Derived,
                    new[] { "particle.age", "particle.lifetime" }, hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Shared/NormalizedAge.hlsl", entryPoint: "FluxFX_NormalizedAge"));
                context.AddProvider(new FluxParticleValueProvider("size.curve", FluxParticleProviderKind.LookupTable,
                    new[] { "size.normalized-age" }, lookupSamples: FluxParticleCurveBaker.Bake(size.Curve)));
                reads = new[] { reads[0], new FluxParticleValueRead("size.curve", FluxParticleReadVersion.Provided) };
                hlsl = "SizeOverLifetimeCurve.hlsl";
            }
            context.AddOperation(new FluxParticleOperationDefinition("render.size-over-lifetime", ModuleId, FluxParticlePhase.Render,
                reads, new[] { new FluxParticleValueWrite("render.size", FluxParticleWriteKind.Modify) },
                new[] { "render.size-start" }, Hlsl + hlsl, "FluxFX_SizeOverLifetime", parameters));
        }
    }
}
