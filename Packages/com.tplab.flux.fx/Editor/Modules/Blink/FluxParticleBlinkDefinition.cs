using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Blink
{
    public sealed class FluxParticleBlinkDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleBlinkModule);

        public string ModuleId => FluxParticleBlinkContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var blink = (FluxParticleBlinkModule)module;
            var contribution = new FluxParticleModuleContribution(context, FluxParticleBlinkContract.Module, blink.Enabled);
            var parameters = new[]
            {
                contribution.Bind(FluxParticleBlinkContract.Frequency, blink.Frequency),
                contribution.Bind(FluxParticleBlinkContract.Duty, blink.Duty),
                contribution.Bind(FluxParticleBlinkContract.Minimum, blink.Minimum),
                contribution.Bind(FluxParticleBlinkContract.Smoothness, blink.Smoothness),
                contribution.Bind(FluxParticleBlinkContract.RandomPhase, blink.RandomPhase),
                contribution.Bind(FluxParticleBlinkContract.TimeMode, blink.TimeMode),
                contribution.Bind(FluxParticleBlinkContract.Target, blink.Target)
            };
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
