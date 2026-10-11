using NUnit.Framework;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Editor.Shaders;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.SceneFlow.Editor.Cores;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public class FluxFXCompileBuildPassTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Compile BuildPass Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void CompilationResultCanBeSharedThroughDataStore()
        {
            AddModule(new FluxParticleGravityModule());

            var registry = new FluxParticleGpuModuleRegistry();
            var compiler = new FluxParticleGpuCompiler(registry);
            var pipeline = new FluxParticleShaderPipeline(registry);

            var plan = FluxParticleCompilePlan.Create(_authoring);
            var result = compiler.Compile(plan);

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            var artifact = pipeline.BuildVelocity(result.Compilation);
            var runtimeJson = new FluxParticleCompiler().Compile(_authoring, plan, result.Compilation);
            var buildResult = new FluxParticleBuildResult(result.Compilation, artifact, runtimeJson);

            var session = new FluxFXCompilationSession();
            session.Add(_authoring, buildResult);

            var data = new SceneFlowDataStore();
            data.Add(session);

            var sharedSession = data.GetRequired<FluxFXCompilationSession>();
            var sharedResult = sharedSession.GetRequired(_authoring);

            Assert.AreSame(session, sharedSession);
            Assert.AreSame(result.Compilation, sharedResult.Compilation);
            Assert.AreSame(artifact, sharedResult.VelocityArtifact);
        }

        [Test]
        public void SharedCompilationPreservesDisabledModule()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var registry = new FluxParticleGpuModuleRegistry();
            var compiler = new FluxParticleGpuCompiler(registry);

            var plan = FluxParticleCompilePlan.Create(_authoring);
            var result = compiler.Compile(plan);

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            var artifact = new FluxParticleShaderPipeline(registry).BuildVelocity(result.Compilation);
            var session = new FluxFXCompilationSession();

            var runtimeJson = new FluxParticleCompiler().Compile(_authoring, plan, result.Compilation);
            session.Add(_authoring, new FluxParticleBuildResult(result.Compilation, artifact, runtimeJson));

            var data = new SceneFlowDataStore();
            data.Add(session);

            var sharedResult = data.GetRequired<FluxFXCompilationSession>().GetRequired(_authoring);
            var stage = sharedResult.Compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            Assert.AreEqual(1, stage.Modules.Count);
            Assert.AreEqual(0.0f, stage.Modules[0].EnabledParameter.FloatValue);
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