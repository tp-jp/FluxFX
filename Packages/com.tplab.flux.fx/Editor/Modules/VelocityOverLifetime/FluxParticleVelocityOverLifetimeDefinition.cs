using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// 時間変化・周回・放射方向の速度を位置へ加算するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleVelocityOverLifetimeDefinition : IFluxParticleModuleDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/VelocityOverLifetime/";
        const string Prefix = "velocity-over-lifetime.";
        public Type ModuleType => typeof(FluxParticleVelocityOverLifetimeModule);
        public string ModuleId => "tplab.fluxfx.velocity-over-lifetime";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var velocity = (FluxParticleVelocityOverLifetimeModule)module;
            context.AddParameter(new FluxParticleParameterDefinition(Prefix + "start", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), velocity.Start);
            context.AddParameter(new FluxParticleParameterDefinition(Prefix + "end", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), velocity.End);
            context.AddParameter(new FluxParticleParameterDefinition(Prefix + "orbital", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), velocity.Orbital);
            context.AddParameter(new FluxParticleParameterDefinition(Prefix + "offset", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), velocity.Offset);
            context.AddParameter(new FluxParticleParameterDefinition(Prefix + "radial", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(velocity.Radial, 0, 0, 0));
            context.AddParameter(new FluxParticleParameterDefinition(Prefix + "enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            context.AddValue(new FluxParticleValueDefinition(Prefix + "center-offset", FluxParticleValueType.Float3,
                FluxParticleWritePolicy.ReadOnly, ModuleId));
            context.AddProvider(new FluxParticleValueProvider(Prefix + "center-offset", FluxParticleProviderKind.Derived,
                new[] { "particle.position" }, new[] { Prefix + "offset" }, Hlsl + "VelocityOverLifetimeOffset.hlsl",
                "FluxFX_VelocityOverLifetimeOffset"));
            context.AddOperation(new FluxParticleOperationDefinition("velocity-over-lifetime", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.AfterOperation, "motion"),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.lifetime", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead(Prefix + "center-offset", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.position", FluxParticleWriteKind.Modify) },
                new[] { "motion" }, Hlsl + "VelocityOverLifetime.hlsl", "FluxFX_VelocityOverLifetime",
                new[] { Prefix + "start", Prefix + "end", Prefix + "orbital", Prefix + "radial" }, Prefix + "enabled"));
        }
    }
}
