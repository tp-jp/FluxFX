using TpLab.Flux.FX.Samples.CustomLift.Udon;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules;
using TpLab.Flux.FX.Editor.Modules.Shared;
using UnityEditor;

namespace TpLab.Flux.FX.Samples.CustomLift.Editor
{
    /// <summary>
    /// Custom Liftの時間変化する加速度を登録します。
    /// </summary>
    public sealed class CustomLiftDefinition : FluxParticleModuleDefinition<CustomLiftModule>
    {
        public override string ModuleId => CustomLiftContract.Module.Id;

        public override void Contribute(CustomLiftModule lift, FluxParticleProgramContribution context)
        {
            var contribution = new FluxParticleModuleContribution(context, CustomLiftContract.Module, lift.Enabled);
            var strength = contribution.Bind(CustomLiftContract.Strength, lift.Strength);
            var direction = contribution.Bind(CustomLiftContract.Direction, lift.Direction);
            if (!context.Providers.ContainsKey("step.time"))
            {
                context.AddProvider(new FluxParticleValueProvider("step.time", FluxParticleProviderKind.Uniform));
            }
            contribution.Operation("custom-lift", FluxParticlePhase.Update,
                reads: new[]
                {
                    new FluxParticleValueRead("particle.velocity", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.time", FluxParticleReadVersion.Provided)
                },
                writes: new[] { new FluxParticleValueWrite("particle.velocity", FluxParticleWriteKind.Modify) },
                hlslPath: AssetDatabase.GUIDToAssetPath("1a634ebd4c1827f4f87bf2fb4d557fe5"), entryPoint: "FluxFX_CustomLift",
                switchable: true, parameters: new[] { strength, direction },
                optionalDependencies: new[] { "gravity", "force", "noise", "vortex", "drag", "limit-velocity" });
            contribution.Complete();
        }
    }
}
