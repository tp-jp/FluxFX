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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationProducesEmptyStage()
        {
            var result = Compile();

            Assert.IsTrue(result.Success, result.Validation.GetReport());
            Assert.IsNotNull(result.Compilation);

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.IsNotNull(stage);
            Assert.AreEqual(0, stage.Modules.Count);
            Assert.AreEqual(0, stage.Parameters.Count);
        }

        [Test]
        public void GravityProducesModuleAndParameters()
        {
            AddModule(new FluxParticleGravityModule());

            var result = Compile();

            Assert.IsTrue(result.Success, result.Validation.GetReport());

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

            Assert.IsTrue(result.Success, result.Validation.GetReport());

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

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            var stage = result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.AreEqual(3, stage.Modules.Count);
            Assert.AreEqual(6, stage.Parameters.Count);

            Assert.AreEqual("tplab.fluxfx.gravity", stage.Modules[0].ModuleId);
            Assert.AreEqual("tplab.fluxfx.force", stage.Modules[1].ModuleId);
            Assert.AreEqual("tplab.fluxfx.drag", stage.Modules[2].ModuleId);
        }

        [Test]
        public void InvalidParameterNameProducesNoCompilation()
        {
            TestGpuModuleDefinition.ParameterName = "invalid-name";

            try
            {
                AddModule(new TestGpuModule());

                var result = Compile();

                Assert.IsFalse(result.Success);
                Assert.IsNull(result.Compilation);

                Assert.IsTrue(HasIssue(result.Validation, "G003"));
            }
            finally
            {
                TestGpuModuleDefinition.ParameterName = "_TestStrength";
            }
        }

        [Test]
        public void DuplicateParameterProducesNoCompilation()
        {
            TestGpuModuleDefinition.DuplicateParameter = true;

            try
            {
                AddModule(new TestGpuModule());

                var result = Compile();

                Assert.IsFalse(result.Success);
                Assert.IsNull(result.Compilation);

                Assert.IsTrue(HasIssue(result.Validation, "G005"));
            }
            finally
            {
                TestGpuModuleDefinition.DuplicateParameter = false;
            }
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