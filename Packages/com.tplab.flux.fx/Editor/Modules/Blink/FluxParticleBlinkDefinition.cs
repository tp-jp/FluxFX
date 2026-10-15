using System;
using UnityEngine;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.Blink
{
    public sealed class FluxParticleBlinkDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleBlinkModule);

        public string ModuleId => "tplab.fluxfx.blink";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var blink = (FluxParticleBlinkModule)module;
            var values = new[] { blink.Frequency, blink.Duty, blink.Minimum, blink.Smoothness, blink.RandomPhase, (float)blink.TimeMode, (float)blink.Target };
            var ids = new[] { "frequency", "duty", "minimum", "smoothness", "random-phase", "time-mode", "target" };
            var maxima = new[] { float.MaxValue, 1f, 1f, 1f, 1f, (float)FluxParticleBlinkTimeMode.ParticleAge, (float)FluxParticleBlinkTarget.Both };
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "blink.enabled", blink.Enabled, values.Length);
            var parameters = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                if (float.IsNaN(values[i]) || float.IsInfinity(values[i]) || values[i] < 0 || values[i] > maxima[i])
                {
                    throw new InvalidOperationException("Invalid Blink parameter: " + ids[i]);
                }
                parameters[i] = contribution.Parameter(i, "blink." + ids[i], FluxParticleValueType.Float,
                    FluxParticleParameterPolicy.Live, new Vector4(values[i], 0, 0, 0));
            }
            if (!context.Providers.ContainsKey("step.time"))
            {
                context.AddProvider(new FluxParticleValueProvider("step.time", FluxParticleProviderKind.Uniform));
            }

            contribution.Operation("render.blink", FluxParticlePhase.Render,
                reads: new[]
                {
                    new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.spawn-key", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("step.time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Blink/Blink.hlsl", entryPoint: "FluxFX_Blink",
                switchable: true, parameters: parameters, optionalDependencies: new[] { "render.color" });
            contribution.Complete();
        }
    }
}
