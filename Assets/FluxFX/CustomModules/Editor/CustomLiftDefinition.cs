using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules.Editor
{
    /// <summary>
    /// Custom Liftの時間変化する加速度を登録します。
    /// </summary>
    public sealed class CustomLiftDefinition : IFluxParticleModuleDefinition, IFluxParticleModuleBindingDefinition
    {
        public Type ModuleType => typeof(CustomLiftModule);
        public string ModuleId => CustomLiftIds.Module;
        public string EnabledParameterId => "custom-lift.enabled";
        public FluxParticleParameterDefinition[] GetHandleParameters(FluxParticleModule module) => new[]
        {
            new FluxParticleParameterDefinition("custom-lift.strength", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live),
            new FluxParticleParameterDefinition("custom-lift.direction", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live)
        };

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var handleParameters = GetHandleParameters(module);
            var lift = (CustomLiftModule)module;
            context.AddParameter(handleParameters[0], new Vector4(lift.Strength, 0, 0, 0));
            context.AddParameter(handleParameters[1], lift.Direction);
            context.AddParameter(new FluxParticleParameterDefinition("custom-lift.enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            if (!context.Providers.ContainsKey("step.time"))
            {
                context.AddProvider(new FluxParticleValueProvider("step.time", FluxParticleProviderKind.Uniform));
            }
            context.AddOperation(new FluxParticleOperationDefinition("custom-lift", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Assets/FluxFX/CustomModules/CustomLift.hlsl", "FluxFX_CustomLift",
                new[] { "custom-lift.strength", "custom-lift.direction" }, "custom-lift.enabled"));
            foreach (var operation in new[] { "gravity", "force", "noise", "vortex", "drag", "limit-velocity" })
            {
                context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("custom-lift", operation));
            }
        }
    }
}
