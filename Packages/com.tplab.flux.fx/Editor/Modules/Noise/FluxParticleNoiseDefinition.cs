using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.Noise
{
    /// <summary>
    /// PositionとSimulation Timeから求めたノイズ加速度をVelocityへ加算します。
    /// </summary>
    public sealed class FluxParticleNoiseDefinition : IFluxParticleModuleDefinition
    {
        const int StrengthSlot = 0;
        const int ScaleSlot = 1;
        const int SpeedSlot = 2;

        public Type ModuleType => typeof(FluxParticleNoiseModule);

        public string ModuleId => "tplab.fluxfx.noise";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var noise = (FluxParticleNoiseModule)module;
            var contribution = new FluxParticleModuleContribution(context, ModuleId, "noise.enabled", noise.Enabled, 3);
            if (noise.Strength < 0 || noise.Scale < 0 || noise.Speed < 0)
            {
                throw new InvalidOperationException("Noise strength, scale and speed must be non-negative.");
            }
            var strengthParameter = contribution.Parameter(StrengthSlot, "noise.strength", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(noise.Strength, 0, 0, 0));
            var scaleParameter = contribution.Parameter(ScaleSlot, "noise.scale", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(noise.Scale, 0, 0, 0));
            var speedParameter = contribution.Parameter(SpeedSlot, "noise.speed", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live, new Vector4(noise.Speed, 0, 0, 0));
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
