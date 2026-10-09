using System;
using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleGpuModuleValidatorTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleGpuModuleValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("GPU Module Validator Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _validator = new FluxParticleGpuModuleValidator(new FluxParticleGpuModuleRegistry());

            TestGpuModuleDefinition.ParameterName = "_TestStrength";
            TestGpuModuleDefinition.DuplicateParameter = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);

            TestGpuModuleDefinition.ParameterName = "_TestStrength";
            TestGpuModuleDefinition.DuplicateParameter = false;
        }

        [Test]
        public void EmptyModules_ShouldPass()
        {
            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
            Assert.IsEmpty(result.Issues);
        }

        [Test]
        public void RegisteredGpuModule_ShouldPass()
        {
            AddModule(new TestGpuModule());

            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
            Assert.IsEmpty(result.Issues);
        }

        [Test]
        public void StandardModules_ShouldPass()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleColorOverLifetimeModule());

            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
            Assert.IsEmpty(result.Issues);
        }

        [Test]
        public void CustomAndStandardModules_ShouldPass()
        {
            AddModule(new TestGpuModule());
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleDragModule());

            var result = Validate();

            Assert.IsTrue(result.IsValid, result.GetReport());
            Assert.IsEmpty(result.Issues);
        }

        [Test]
        public void UnregisteredModule_ShouldReportG001()
        {
            AddModule(new UnregisteredGpuModule());

            AssertIssue("G001");
        }

        [Test]
        public void UnsupportedStage_ShouldReportG002()
        {
            AddModule(new UnsupportedStageGpuModule());

            AssertIssue("G002");
        }

        [Test]
        public void InvalidParameterName_ShouldReportG003()
        {
            TestGpuModuleDefinition.ParameterName = "invalid-name";
            AddModule(new TestGpuModule());

            AssertIssue("G003");
        }

        [Test]
        public void BuiltInParameterCollision_ShouldReportG004()
        {
            TestGpuModuleDefinition.ParameterName = "_Gravity";
            AddModule(new TestGpuModule());

            AssertIssue("G004");
        }

        [Test]
        public void DuplicateParameter_ShouldReportG005()
        {
            TestGpuModuleDefinition.DuplicateParameter = true;
            AddModule(new TestGpuModule());

            AssertIssue("G005");
        }

        void AssertIssue(string expectedCode)
        {
            var result = Validate();

            Assert.IsTrue(result.HasErrors, $"Expected {expectedCode}.");

            foreach (var issue in result.Issues)
            {
                if (issue.Code == expectedCode) return;
            }

            Assert.Fail($"Expected {expectedCode}.\n{result.GetReport()}");
        }

        FluxParticleValidationResult Validate()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);
            return _validator.Validate(plan);
        }

        void AddModule(FluxParticleModule module)
        {
            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");

            var index = modules.arraySize;
            modules.arraySize++;

            modules.GetArrayElementAtIndex(index).managedReferenceValue = module;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

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

    public sealed class TestGpuModuleDefinition : FluxParticleGpuModuleDefinition
    {
        public static string ParameterName = "_TestStrength";
        public static bool DuplicateParameter;

        public override Type ModuleType => typeof(TestGpuModule);

        public override string ModuleId => "test.fluxfx.validator";

        public override string HlslPath => "Assets/FluxFX/CustomModules/CustomLift.hlsl";

        public override string EntryPoint => "FluxFX_CustomLift";

        public override int Order => 150;

        public override void CollectParameters(FluxParticleModule module, FluxParticleParameterCollector collector)
        {
            collector.AddFloat(ParameterName, 1.0f);

            if (DuplicateParameter)
            {
                collector.AddFloat(ParameterName, 2.0f);
            }
        }
    }

    public sealed class UnsupportedStageGpuModuleDefinition : FluxParticleGpuModuleDefinition
    {
        public override Type ModuleType => typeof(UnsupportedStageGpuModule);

        public override string ModuleId => "test.fluxfx.unsupported-stage";

        public override string HlslPath => "Assets/FluxFX/CustomModules/CustomLift.hlsl";

        public override string EntryPoint => "FluxFX_CustomLift";

        public override int Order => 200;
    }
}