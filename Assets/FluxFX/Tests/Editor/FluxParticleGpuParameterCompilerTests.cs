using Newtonsoft.Json.Linq;
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
        public void DisabledModulePreservesParameters()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");
            modules.GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var velocity = Compile()["velocity"];

            var registry = new FluxParticleGpuModuleRegistry();
            var definition = registry.Get(typeof(FluxParticleGravityModule));
            var enabledName = FluxParticleGpuModuleEnabled.GetParameterName(definition.ModuleId);

            Assert.IsNotNull(velocity["_Gravity"]);
            Assert.IsNotNull(velocity[enabledName]);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)velocity[enabledName]["type"]);
            Assert.AreEqual(0.0f, (float)velocity[enabledName]["value"]);
        }

        [Test]
        public void MultipleModulesProduceIndependentParameters()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            var velocity = Compile()["velocity"];
            var registry = new FluxParticleGpuModuleRegistry();

            Assert.IsNotNull(velocity["_Gravity"]);
            Assert.IsNotNull(velocity["_Force"]);
            Assert.IsNotNull(velocity["_Drag"]);
            Assert.IsNotNull(velocity["_MaxSpeed"]);

            var moduleTypes = new[]
            {
                typeof(FluxParticleGravityModule),
                typeof(FluxParticleForceModule),
                typeof(FluxParticleDragModule),
                typeof(FluxParticleLimitVelocityModule)
            };

            foreach (var moduleType in moduleTypes)
            {
                var definition = registry.Get(moduleType);
                var enabledName = FluxParticleGpuModuleEnabled.GetParameterName(definition.ModuleId);

                Assert.IsNotNull(velocity[enabledName], $"Enabled parameter missing: {definition.ModuleId}");
                Assert.AreEqual((int)FluxParticleParameterType.Float, (int)velocity[enabledName]["type"]);
                Assert.AreEqual(1.0f, (float)velocity[enabledName]["value"]);
            }

            Assert.AreEqual(8, velocity.ToObject<JObject>().Count);
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