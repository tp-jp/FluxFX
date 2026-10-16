using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Noise
{
    /// <summary>
    /// PositionとSimulation Timeから求めたノイズ加速度をVelocityへ加算します。
    /// </summary>
    public sealed class FluxParticleNoiseDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleNoiseModule);

        public string ModuleId => FluxParticleNoiseContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var noise = (FluxParticleNoiseModule)module;
            var contribution = new FluxParticleModuleContribution(context, FluxParticleNoiseContract.Module, noise.Enabled);
            var strengthParameter = contribution.Bind(FluxParticleNoiseContract.Strength, noise.Strength);
            var scaleParameter = contribution.Bind(FluxParticleNoiseContract.Scale, noise.Scale);
            var speedParameter = contribution.Bind(FluxParticleNoiseContract.Speed, noise.Speed);
            if (!context.Providers.ContainsKey("step.time"))
            {
                context.AddProvider(new FluxParticleValueProvider("step.time", FluxParticleProviderKind.Uniform));
            }
            contribution.Operation("noise", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Noise/Noise.hlsl", entryPoint: "FluxFX_Noise",
                switchable: true, parameters: new[] { strengthParameter, scaleParameter, speedParameter },
                optionalDependencies: new[] { "gravity", "force" });
            contribution.Complete();
        }
    }
}
