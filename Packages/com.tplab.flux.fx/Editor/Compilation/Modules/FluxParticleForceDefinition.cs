using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// 一定の加速度をVelocityへ加算するOperationを提供します。
    /// </summary>
    public sealed class FluxParticleForceDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleForceModule);
        public string ModuleId => "tplab.fluxfx.force";

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var force = (FluxParticleForceModule)module;
            context.AddParameter(new FluxParticleParameterDefinition("force", FluxParticleValueType.Float3,
                FluxParticleParameterPolicy.Live), force.Force);
            context.AddParameter(new FluxParticleParameterDefinition("force.enabled", FluxParticleValueType.Float,
                FluxParticleParameterPolicy.Live), new Vector4(1, 0, 0, 0));
            context.AddOperation(new FluxParticleOperationDefinition("force", ModuleId, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Force.hlsl", "FluxFX_Force",
                new[] { "force" }, "force.enabled"));
            context.AddOptionalDependency(new FluxParticleOptionalOperationDependency("force", "gravity"));
        }
    }
}
