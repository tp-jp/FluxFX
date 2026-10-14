using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// PositionとSimulation Timeから求めたノイズ加速度をVelocityへ加算します。
    /// </summary>
    public sealed class FluxParticleNoiseDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleNoiseModule);
        public string ModuleId => "tplab.fluxfx.noise";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var noise = (FluxParticleNoiseModule)module;
            if (noise.Strength < 0 || noise.Scale < 0 || noise.Speed < 0)
            {
                throw new InvalidOperationException("Noise strength, scale and speed must be non-negative.");
            }
            context.AddParameter(new FluxParticleParameterDefinition("noise.strength", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(noise.Strength, 0, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition("noise.scale", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(noise.Scale, 0, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition("noise.speed", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(noise.Speed, 0, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition("noise.enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            if (!context.Providers.ContainsKey("step.time"))
            {
                context.AddProvider(new FluxParticleValueProvider("step.time", FluxParticleProviderKind.Uniform));
            }
            context.AddOperation(new FluxParticleOperationDefinition("noise", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Noise.hlsl", "FluxFX_Noise",
                new[] { "noise.strength", "noise.scale", "noise.speed" }, "noise.enabled"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("noise", "gravity"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("noise", "force"));
        }
    }
}
