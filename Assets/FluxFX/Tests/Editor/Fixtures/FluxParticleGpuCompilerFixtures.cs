using System;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    [Serializable]
    public sealed class TestGpuModule : FluxParticleModule
    {
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.Velocity;
    }

    [Serializable]
    public sealed class UnregisteredGpuModule : FluxParticleModule
    {
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.Velocity;
    }

    [Serializable]
    public sealed class UnsupportedStageGpuModule : FluxParticleModule
    {
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.PositionUpdate;

        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Position;

        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.Position;
    }

    public sealed class TestGpuModuleDefinition : FluxParticleGpuModuleDefinition<TestGpuModule>
    {
        public static string ParameterName = "_TestStrength";
        public static bool DuplicateParameter;

        public override string ModuleId => "test.fluxfx.validator";

        public override string HlslPath => "Assets/FluxFX/CustomModules/CustomLift.hlsl";

        public override string EntryPoint => "FluxFX_CustomLift";

        public override int Order => 150;

        public override void CollectParameters(TestGpuModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat(ParameterName, 1.0f);

            if (DuplicateParameter)
            {
                collector.AddFloat(ParameterName, 2.0f);
            }
        }
    }

    public sealed class UnsupportedStageGpuModuleDefinition : FluxParticleGpuModuleDefinition<UnsupportedStageGpuModule>
    {
        public override string ModuleId => "test.fluxfx.unsupported-stage";

        public override string HlslPath => "Assets/FluxFX/CustomModules/CustomLift.hlsl";

        public override string EntryPoint => "FluxFX_CustomLift";

        public override int Order => 200;
    }
}