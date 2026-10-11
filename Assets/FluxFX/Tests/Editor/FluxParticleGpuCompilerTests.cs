using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleGpuCompilerTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleGpuCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("GPU Compiler Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleGpuCompiler(new FluxParticleGpuModuleRegistry());

            TestGpuModuleDefinition.ParameterName = "_TestStrength";
            TestGpuModuleDefinition.DuplicateParameter = false;
            TestPositionGpuModuleDefinition.ParameterName = "_TestPositionStrength";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);

            TestGpuModuleDefinition.ParameterName = "_TestStrength";
            TestGpuModuleDefinition.DuplicateParameter = false;
            TestPositionGpuModuleDefinition.ParameterName = "_TestPositionStrength";
        }

        [Test]
        public void EmptyConfigurationProducesEmptyStage()
        {
            var result = Compile();

            AssertSuccess(result);

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.IsNotNull(stage);
            Assert.AreEqual(0, stage.Modules.Count);
            Assert.AreEqual(0, stage.Parameters.Count);
            Assert.IsEmpty(result.Validation.Issues);
        }

        [Test]
        public void RegisteredGpuModuleCompilesSuccessfully()
        {
            AddModule(new TestGpuModule());

            var result = Compile();

            AssertSuccess(result);
            Assert.IsEmpty(result.Validation.Issues);

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.AreEqual(1, stage.Modules.Count);
            Assert.AreEqual("test.fluxfx.validator", stage.Modules[0].ModuleId);
        }

        [Test]
        public void StandardAndLegacyModulesCompileSuccessfully()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleColorOverLifetimeModule());

            var result = Compile();

            AssertSuccess(result);
            Assert.IsEmpty(result.Validation.Issues);

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.AreEqual(3, stage.Modules.Count);
        }

        [Test]
        public void CustomAndStandardModulesCompileSuccessfully()
        {
            AddModule(new TestGpuModule());
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleDragModule());

            var result = Compile();

            AssertSuccess(result);
            Assert.IsEmpty(result.Validation.Issues);

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.AreEqual(3, stage.Modules.Count);
        }

        [Test]
        public void GravityProducesModuleAndParameters()
        {
            AddModule(new FluxParticleGravityModule());

            var result = Compile();

            AssertSuccess(result);

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var module = stage.Modules[0];

            Assert.AreEqual("tplab.fluxfx.gravity", module.ModuleId);
            Assert.AreEqual(1, module.Parameters.Count);
            Assert.AreEqual("_Gravity", module.Parameters[0].Name);
            Assert.AreEqual(FluxParticleParameterType.Vector, module.Parameters[0].Type);

            Assert.AreEqual(FluxParticleParameterType.Float, module.EnabledParameter.Type);
            Assert.AreEqual(1.0f, module.EnabledParameter.FloatValue);

            Assert.AreEqual(2, stage.Parameters.Count);
        }

        [Test]
        public void DisabledModulePreservesParameters()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("enabled")
                .boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var result = Compile();

            AssertSuccess(result);

            var module = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate).Modules[0];

            Assert.AreEqual(1, module.Parameters.Count);
            Assert.AreEqual("_Gravity", module.Parameters[0].Name);
            Assert.AreEqual(0.0f, module.EnabledParameter.FloatValue);
        }

        [Test]
        public void MultipleModulesHaveIndependentParameters()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleDragModule());

            var result = Compile();

            AssertSuccess(result);

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.AreEqual(3, stage.Modules.Count);
            Assert.AreEqual(6, stage.Parameters.Count);

            Assert.AreEqual("tplab.fluxfx.gravity", stage.Modules[0].ModuleId);
            Assert.AreEqual("tplab.fluxfx.force", stage.Modules[1].ModuleId);
            Assert.AreEqual("tplab.fluxfx.drag", stage.Modules[2].ModuleId);
        }

        [Test]
        public void UnregisteredModuleProducesG001()
        {
            AddModule(new UnregisteredGpuModule());

            AssertFailure("G001");
        }

        [Test]
        public void UnsupportedStageProducesG002()
        {
            AddModule(new UnsupportedStageGpuModule());

            AssertFailure("G002");
        }

        [Test]
        public void InvalidParameterNameProducesG003()
        {
            TestGpuModuleDefinition.ParameterName = "invalid-name";

            AddModule(new TestGpuModule());

            AssertFailure("G003");
        }

        [Test]
        public void BuiltInParameterCollisionProducesG004()
        {
            TestGpuModuleDefinition.ParameterName = "_DeltaTime";

            AddModule(new TestGpuModule());

            AssertFailure("G004");
        }

        [Test]
        public void DuplicateParameterProducesG005()
        {
            TestGpuModuleDefinition.DuplicateParameter = true;

            AddModule(new TestGpuModule());

            AssertFailure("G005");
        }

        [Test]
        public void EmptyConfigurationProducesBothStages()
        {
            var result = Compile();

            AssertSuccess(result);

            Assert.AreEqual(2, result.Compilation.Stages.Count);

            var velocity = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var position = result.Compilation.GetStage(FluxParticleExecutionStage.PositionUpdate);

            Assert.IsNotNull(velocity);
            Assert.IsNotNull(position);
            Assert.IsEmpty(velocity.Modules);
            Assert.IsEmpty(position.Modules);
            Assert.IsEmpty(velocity.Parameters);
            Assert.IsEmpty(position.Parameters);
        }

        [Test]
        public void PositionGpuModuleCompilesSuccessfully()
        {
            AddModule(new TestPositionGpuModule());

            var result = Compile();

            AssertSuccess(result);

            var velocity = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var position = result.Compilation.GetStage(FluxParticleExecutionStage.PositionUpdate);

            Assert.IsEmpty(velocity.Modules);
            Assert.AreEqual(1, position.Modules.Count);
            Assert.AreEqual("test.fluxfx.position", position.Modules[0].ModuleId);
            Assert.AreEqual(FluxParticleExecutionStage.PositionUpdate, position.Modules[0].Stage);
            Assert.AreEqual("_TestPositionStrength", position.Modules[0].Parameters[0].Name);
        }

        [Test]
        public void VelocityAndPositionModulesCompileTogether()
        {
            AddModule(new TestGpuModule());
            AddModule(new TestPositionGpuModule());

            var result = Compile();

            AssertSuccess(result);

            var velocity = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var position = result.Compilation.GetStage(FluxParticleExecutionStage.PositionUpdate);

            Assert.AreEqual(1, velocity.Modules.Count);
            Assert.AreEqual(1, position.Modules.Count);
            Assert.AreEqual("test.fluxfx.validator", velocity.Modules[0].ModuleId);
            Assert.AreEqual("test.fluxfx.position", position.Modules[0].ModuleId);
        }

        [Test]
        public void SameParameterNameAcrossStagesIsAllowed()
        {
            TestGpuModuleDefinition.ParameterName = "_SharedStrength";
            TestPositionGpuModuleDefinition.ParameterName = "_SharedStrength";

            AddModule(new TestGpuModule());
            AddModule(new TestPositionGpuModule());

            var result = Compile();

            AssertSuccess(result);

            var velocity = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var position = result.Compilation.GetStage(FluxParticleExecutionStage.PositionUpdate);

            Assert.AreEqual("_SharedStrength", velocity.Modules[0].Parameters[0].Name);
            Assert.AreEqual("_SharedStrength", position.Modules[0].Parameters[0].Name);
        }

        [Test]
        public void PositionBuiltInParameterCollisionProducesG004()
        {
            TestPositionGpuModuleDefinition.ParameterName = "_DeltaTime";
            AddModule(new TestPositionGpuModule());

            AssertFailure("G004");
        }

        [Test]
        public void PositionShaderSpecificParameterCollisionProducesG004()
        {
            TestPositionGpuModuleDefinition.ParameterName = "_ShapeRadius";
            AddModule(new TestPositionGpuModule());

            AssertFailure("G004");
        }

        [Test]
        public void PositionInvalidParameterNameProducesG003()
        {
            TestPositionGpuModuleDefinition.ParameterName = "invalid-position-name";
            AddModule(new TestPositionGpuModule());

            AssertFailure("G003");
        }

        [Test]
        public void LegacyPositionModuleDoesNotBecomeGpuModule()
        {
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            var result = Compile();

            AssertSuccess(result);

            var position = result.Compilation.GetStage(FluxParticleExecutionStage.PositionUpdate);

            Assert.IsNotNull(position);
            Assert.IsEmpty(position.Modules);
        }

        void AssertFailure(string expectedCode)
        {
            var result = Compile();

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Compilation);
            Assert.IsTrue(result.Validation.HasErrors);
            Assert.IsTrue(HasIssue(result.Validation, expectedCode),
                $"Expected {expectedCode}.\n{result.Validation.GetReport()}");
        }

        static void AssertSuccess(FluxParticleGpuCompileResult result)
        {
            Assert.IsTrue(result.Success, result.Validation.GetReport());
            Assert.IsNotNull(result.Compilation);
        }

        FluxParticleGpuCompileResult Compile()
        {
            return _compiler.Compile(FluxParticleCompilePlan.Create(_authoring));
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

        static bool HasIssue(FluxParticleValidationResult validation, string code)
        {
            foreach (var issue in validation.Issues)
            {
                if (issue.Code == code) return true;
            }

            return false;
        }
    }
}