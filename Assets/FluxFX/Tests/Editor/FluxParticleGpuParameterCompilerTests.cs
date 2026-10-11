using Newtonsoft.Json.Linq;
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
    public class FluxParticleGpuParameterCompilerTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleGpuParameterCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("GPU Parameter Compiler Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleGpuParameterCompiler(new FluxParticleGpuModuleRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationProducesEmptyVelocityParameters()
        {
            var result = Compile();

            Assert.AreEqual(0, result["velocity"].ToObject<JObject>().Count);
        }

        [Test]
        public void GravityProducesVectorParameter()
        {
            AddModule(new FluxParticleGravityModule());

            var velocity = Compile()["velocity"];
            var gravity = velocity["_Gravity"];

            Assert.IsNotNull(gravity);
            Assert.AreEqual((int)FluxParticleParameterType.Vector, (int)gravity["type"]);
            Assert.AreEqual(4, gravity["value"].ToObject<JArray>().Count);
        }

        [Test]
        public void DragProducesFloatParameter()
        {
            var module = new FluxParticleDragModule();
            AddModule(module);

            var velocity = Compile()["velocity"];
            var drag = velocity["_Drag"];

            Assert.IsNotNull(drag);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)drag["type"]);
            Assert.AreEqual(module.Drag, (float)drag["value"]);
        }

        [Test]
        public void DisabledModuleIsExcluded()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");
            modules.GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var velocity = Compile()["velocity"];

            Assert.IsNull(velocity["_Gravity"]);
        }

        [Test]
        public void MultipleModulesProduceIndependentParameters()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            var velocity = Compile()["velocity"];

            Assert.IsNotNull(velocity["_Gravity"]);
            Assert.IsNotNull(velocity["_Force"]);
            Assert.IsNotNull(velocity["_Drag"]);
            Assert.IsNotNull(velocity["_MaxSpeed"]);
            Assert.AreEqual(4, velocity.ToObject<JObject>().Count);
        }

        JObject Compile()
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
    }
}