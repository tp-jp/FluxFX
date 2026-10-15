using System;
using System.Linq;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

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
                "Packages/com.tplab.flux.fx/Editor/Modules/ColorOverLifetime/RenderColor.hlsl", "FluxFX_RenderColor",
                new[] { "color.end" }));
        }

        void AddGradient(FluxParticleColorOverLifetimeModule color, FluxParticleProgramContribution context)
        {
            if (color.Gradient == null || (color.Gradient.mode != UnityEngine.GradientMode.Blend && color.Gradient.mode != UnityEngine.GradientMode.Fixed))
            {
                throw new InvalidOperationException("Unsupported color gradient mode.");
            }
            var positions = color.Gradient.mode == UnityEngine.GradientMode.Fixed
                ? color.Gradient.colorKeys.Select(key => key.time).Concat(color.Gradient.alphaKeys.Select(key => key.time))
                    .Concat(new[] { 0f, 1f }).Distinct().OrderBy(time => time).ToArray()
                : Array.Empty<float>();
            var samples = new UnityEngine.Vector4[positions.Length != 0 ? positions.Length : 128];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = positions.Length == 0 ? i / (float)(samples.Length - 1)
                    : i == 0 ? positions[0] : (positions[i - 1] + positions[i]) * 0.5f;
                samples[i] = color.Gradient.Evaluate(time);
            }
            context.AddValue(new FluxParticleValueDefinition("color.normalized-age", FluxParticleValueType.Float,
                FluxParticleWritePolicy.ReadOnly, ModuleId));
            context.AddValue(new FluxParticleValueDefinition("color.gradient", FluxParticleValueType.Float4,
                FluxParticleWritePolicy.ReadOnly, ModuleId));
            context.AddProvider(new FluxParticleValueProvider("color.normalized-age", FluxParticleProviderKind.Derived,
                new[] { "particle.age", "particle.lifetime" },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Shared/NormalizedAge.hlsl", entryPoint: "FluxFX_NormalizedAge"));
            context.AddProvider(new FluxParticleValueProvider("color.gradient", FluxParticleProviderKind.LookupTable,
                new[] { "color.normalized-age" }, lookupVectorSamples: samples, lookupPositions: positions));
            context.AddOperation(new FluxParticleOperationDefinition("render.color", ModuleId, FluxParticlePhase.Render,
                new[]
                {
                    new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    new FluxParticleValueRead("color.gradient", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                new[] { "render.start" },
                "Packages/com.tplab.flux.fx/Editor/Modules/ColorOverLifetime/RenderColorGradient.hlsl", "FluxFX_RenderColorGradient"));
        }
    }
}