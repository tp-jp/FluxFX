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
            if (color.Mode == FluxParticleColorOverLifetimeMode.Gradient)
            {
                AddGradient(color, context);
                return;
            }
            if (color.Mode != FluxParticleColorOverLifetimeMode.Linear)
            {
                throw new InvalidOperationException("Unsupported color over lifetime mode.");
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

        void AddGradient(FluxParticleColorOverLifetimeModule color, FluxParticleProgramContribution context)
        {
            if (color.Gradient == null || color.Gradient.mode != UnityEngine.GradientMode.Blend)
            {
                throw new InvalidOperationException("Color gradient requires Blend mode.");
            }
            var samples = new UnityEngine.Vector4[128];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = color.Gradient.Evaluate(i / (float)(samples.Length - 1));
            }
            context.AddValue(new FluxParticleValueDefinition("color.normalized-age", FluxParticleValueType.Float,
                FluxParticleWritePolicy.ReadOnly, ModuleId));
            context.AddValue(new FluxParticleValueDefinition("color.gradient", FluxParticleValueType.Float4,
                FluxParticleWritePolicy.ReadOnly, ModuleId));
            context.AddProvider(new FluxParticleValueProvider("color.normalized-age", FluxParticleProviderKind.Derived,
                new[] { "particle.age", "particle.lifetime" },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/NormalizedAge.hlsl", entryPoint: "FluxFX_NormalizedAge"));
            context.AddProvider(new FluxParticleValueProvider("color.gradient", FluxParticleProviderKind.LookupTable,
                new[] { "color.normalized-age" }, lookupVectorSamples: samples));
            context.AddOperation(new FluxParticleOperationDefinition("render.color", ModuleId, FluxParticlePhase.Render,
                new[]
                {
                    new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    new FluxParticleValueRead("color.gradient", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                new[] { "render.start" },
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/RenderColorGradient.hlsl", "FluxFX_RenderColorGradient"));
        }    }
}