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
    public class FluxParticleGpuModuleBindingCompilerTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleGpuModuleBindingCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("GPU Module Binding Compiler Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleGpuModuleBindingCompiler(new FluxParticleGpuModuleRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationProducesEmptyBindings()
        {
            var bindings = Compile();

            Assert.AreEqual(0, bindings.Count);
        }

        [Test]
        public void GravityProducesBinding()
        {
            AddModule(new FluxParticleGravityModule());

            var bindings = Compile();
            var binding = bindings["tplab.fluxfx.gravity"];

            Assert.IsNotNull(binding);
            Assert.AreEqual((int)FluxParticleKernelTarget.Velocity, (int)binding["target"]);
            Assert.AreEqual(
                FluxParticleGpuModuleEnabled.GetParameterName("tplab.fluxfx.gravity"),
                (string)binding["enabledParameter"]);
        }

        [Test]
        public void DisabledModuleStillProducesBinding()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");
            modules.GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var bindings = Compile();

            Assert.IsNotNull(bindings["tplab.fluxfx.gravity"]);
        }

        [Test]
        public void MultipleModulesProduceIndependentBindings()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleVortexModule());

            var bindings = Compile();

            Assert.AreEqual(3, bindings.Count);

            Assert.IsNotNull(bindings["tplab.fluxfx.gravity"]);
            Assert.IsNotNull(bindings["tplab.fluxfx.noise"]);
            Assert.IsNotNull(bindings["tplab.fluxfx.vortex"]);
        }

        Newtonsoft.Json.Linq.JObject Compile()
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