using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Rotation over LifetimeのRender Operationを提供します。
    /// </summary>
    public sealed class FluxParticleRotationOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/";

        public Type ModuleType => typeof(FluxParticleRotationOverLifetimeModule);
        public string ModuleId => "tplab.fluxfx.rotation-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var rotation = (FluxParticleRotationOverLifetimeModule)module;
            if (rotation.Mode != FluxParticleRotationOverLifetimeMode.Linear &&
                rotation.Mode != FluxParticleRotationOverLifetimeMode.Curve)
            {
                throw new InvalidOperationException("Unsupported rotation over lifetime mode.");
            }
            var end = rotation.EndRotation;
            if (context.RenderMode == FluxParticleRenderMode.Billboard && (end.x != 0 || end.y != 0))
            {
                throw new InvalidOperationException("Program Backend currently requires: Billboard rotation over lifetime around Z only.");
            }

            context.AddParameter(new FluxParticleParameterDefinition("rotation.end", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), end);
            var reads = new[]
            {
                new FluxParticleValueRead("render.rotation", FluxParticleReadVersion.AfterOperation, "render.rotation-start"),
                new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided)
            };
            var hlsl = "AuthoringRotationOverLifetimeLinear.hlsl";
            if (rotation.Mode == FluxParticleRotationOverLifetimeMode.Curve)
            {
                var samples = FluxParticleCurveBaker.Bake(rotation.Curve);
                context.AddValue(new FluxParticleValueDefinition("rotation.normalized-age", FluxParticleValueType.Float,
                    FluxParticleWritePolicy.ReadOnly, ModuleId));
                context.AddValue(new FluxParticleValueDefinition("rotation.curve", FluxParticleValueType.Float,
                    FluxParticleWritePolicy.ReadOnly, ModuleId));
                context.AddProvider(new FluxParticleValueProvider("rotation.normalized-age", FluxParticleProviderKind.Derived,
                    new[] { "particle.age", "particle.lifetime" }, hlslPath: Hlsl + "NormalizedAge.hlsl", entryPoint: "FluxFX_NormalizedAge"));
                context.AddProvider(new FluxParticleValueProvider("rotation.curve", FluxParticleProviderKind.LookupTable,
                    new[] { "rotation.normalized-age" }, lookupSamples: samples));
                reads = new[]
                {
                    reads[0],
                    new FluxParticleValueRead("rotation.curve", FluxParticleReadVersion.Provided)
                };
                hlsl = "AuthoringRotationOverLifetimeCurve.hlsl";
            }
            context.AddOperation(new FluxParticleOperationDefinition("render.rotation-over-lifetime", ModuleId, FluxParticlePhase.Render,
                reads, new[] { new FluxParticleValueWrite("render.rotation", FluxParticleWriteKind.Modify) },
                new[] { "render.rotation-start" }, Hlsl + hlsl,
                "FluxFX_RenderRotationOverLifetime", new[] { "rotation.end" }));
        }
    }
}
