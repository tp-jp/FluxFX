using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxParticleGpuMaterialBinderTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("GPU Material Binder Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_material != null) Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void GravityParametersAreApplied()
        {
            AddModule(new FluxParticleGravityModule());
            var stage = CreateMaterial();

            new FluxParticleGpuMaterialBinder().Apply(stage, _material);

            Assert.AreEqual(new Vector4(0, -1, 0, 0), _material.GetVector("_Gravity"));
            Assert.AreEqual(1.0f, _material.GetFloat(stage.Modules[0].EnabledParameter.Name));
        }

        [Test]
        public void DisabledModulePreservesValues()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var stage = CreateMaterial();

            new FluxParticleGpuMaterialBinder().Apply(stage, _material);

            Assert.AreEqual(new Vector4(0, -1, 0, 0), _material.GetVector("_Gravity"));
            Assert.AreEqual(0.0f, _material.GetFloat(stage.Modules[0].EnabledParameter.Name));
        }

        [Test]
        public void ApplyingNewCompilationUpdatesExistingMaterial()
        {
            AddModule(new FluxParticleGravityModule());
            var first = CreateMaterial();
            var binder = new FluxParticleGpuMaterialBinder();

            binder.Apply(first, _material);

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("gravity").vector3Value = new Vector3(2, -5, 3);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var result = new FluxParticleGpuCompiler(new FluxParticleGpuModuleRegistry())
                .Compile(FluxParticleCompilePlan.Create(_authoring));

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            binder.Apply(result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate), _material);

            Assert.AreEqual(new Vector4(2, -5, 3, 0), _material.GetVector("_Gravity"));
        }

        FluxParticleGpuStageCompilation CreateMaterial()
        {
            var registry = new FluxParticleGpuModuleRegistry();
            var result = new FluxParticleGpuCompiler(registry).Compile(FluxParticleCompilePlan.Create(_authoring));

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            var pipeline = new FluxParticleShaderPipeline(registry);
            var artifact = pipeline.BuildVelocity(result.Compilation);
            var shader = pipeline.GetOrCreate(artifact);

            Assert.IsNotNull(shader);

            _material = new Material(shader);

            return result.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
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
}