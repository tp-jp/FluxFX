using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Modules.VelocityOverLifetime
{
    /// <summary>
    /// 時間変化・周回・放射方向の速度を位置へ加算するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleVelocityOverLifetimeDefinition : IFluxParticleModuleDefinition, IFluxParticleModuleBindingDefinition
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/VelocityOverLifetime/";
        const string Prefix = "velocity-over-lifetime.";
        public Type ModuleType => typeof(FluxParticleVelocityOverLifetimeModule);
        public string ModuleId => "tplab.fluxfx.velocity-over-lifetime";
        public string EnabledParameterId => "velocity-over-lifetime.enabled";
        public FluxParticleParameterDefinition[] GetHandleParameters(FluxParticleModule module) => new FluxParticleParameterDefinition[]
        {
            new FluxParticleParameterDefinition("velocity-over-lifetime.start", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live),
            new FluxParticleParameterDefinition("velocity-over-lifetime.end", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live),
            new FluxParticleParameterDefinition("velocity-over-lifetime.orbital", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live),
            new FluxParticleParameterDefinition("velocity-over-lifetime.offset", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live),
            new FluxParticleParameterDefinition("velocity-over-lifetime.radial", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live)
        };

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var handleParameters = GetHandleParameters(module);
            var velocity = (FluxParticleVelocityOverLifetimeModule)module;
            context.AddParameter(handleParameters[0], velocity.Start);
            context.AddParameter(handleParameters[1], velocity.End);
            context.AddParameter(handleParameters[2], velocity.Orbital);
            context.AddParameter(handleParameters[3], velocity.Offset);
            context.AddParameter(handleParameters[4], new Vector4(velocity.Radial, 0, 0, 0));
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
