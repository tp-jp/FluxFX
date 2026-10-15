using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts.Modules;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Vortex
{
    /// <summary>
    /// Step開始時のPositionから接線方向の加速度をVelocityへ加算します。
    /// </summary>
    public sealed class FluxParticleVortexDefinition : IFluxParticleModuleDefinition
    {
        public Type ModuleType => typeof(FluxParticleVortexModule);

        public string ModuleId => FluxParticleVortexContract.Module.Id;

        public void Contribute(FluxParticleModule module, FluxParticleProgramContribution context)
        {
            var vortex = (FluxParticleVortexModule)module;
            var contribution = new FluxParticleModuleContribution(context, FluxParticleVortexContract.Module, vortex.Enabled);
            var centerParameter = contribution.Bind(FluxParticleVortexContract.Center, vortex.Center);
            var axisParameter = contribution.Bind(FluxParticleVortexContract.Axis, vortex.Axis);
            var strengthParameter = contribution.Bind(FluxParticleVortexContract.Strength, vortex.Strength);
            contribution.Operation("vortex", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: "Packages/com.tplab.flux.fx/Editor/Modules/Vortex/Vortex.hlsl", entryPoint: "FluxFX_Vortex",
                switchable: true, parameters: new[] { centerParameter, axisParameter, strengthParameter },
                optionalDependencies: new[] { "gravity", "force", "noise" });
            contribution.Complete();
        }
    }
}
