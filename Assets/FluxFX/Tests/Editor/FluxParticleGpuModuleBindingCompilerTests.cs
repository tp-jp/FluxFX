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

        [Test]
        public void GravityBindingContainsOwnedParameters()
        {
            AddModule(new FluxParticleGravityModule());

            var binding = Compile()["tplab.fluxfx.gravity"];
            var parameters = binding["parameters"];

            Assert.IsNotNull(parameters);
            Assert.AreEqual(1, ((JObject)parameters).Count);
            Assert.AreEqual((int)FluxParticleParameterType.Vector, (int)parameters["_Gravity"]);
        }

        [Test]
        public void CustomModuleParametersAreIndependent()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());

            var bindings = Compile();
            var gravity = bindings["tplab.fluxfx.gravity"]["parameters"];
            var force = bindings["tplab.fluxfx.force"]["parameters"];

            Assert.IsNotNull(gravity["_Gravity"]);
            Assert.IsNull(gravity["_Force"]);

            Assert.IsNotNull(force["_Force"]);
            Assert.IsNull(force["_Gravity"]);
        }

        [Test]
        public void MissingParameterIsRejected()
        {
            AddModule(new FluxParticleGravityModule());

            var plan = FluxParticleCompilePlan.Create(_authoring);
            var registry = new FluxParticleGpuModuleRegistry();
            var gpuParameters = new FluxParticleGpuParameterCompiler(registry).Compile(plan);

            ((JObject)gpuParameters["velocity"]).Remove("_Gravity");

            var exception = Assert.Throws<System.InvalidOperationException>(() => _compiler.Compile(plan, gpuParameters));

            StringAssert.Contains("_Gravity", exception.Message);
        }

        [Test]
        public void ParameterTypeMismatchIsRejected()
        {
            AddModule(new FluxParticleGravityModule());

            var plan = FluxParticleCompilePlan.Create(_authoring);
            var registry = new FluxParticleGpuModuleRegistry();
            var gpuParameters = new FluxParticleGpuParameterCompiler(registry).Compile(plan);

            gpuParameters["velocity"]["_Gravity"]["type"] = (int)FluxParticleParameterType.Float;

            var exception = Assert.Throws<System.InvalidOperationException>(() => _compiler.Compile(plan, gpuParameters));

            StringAssert.Contains("_Gravity", exception.Message);
        }

        [Test]
        public void UnownedParameterIsRejected()
        {
            AddModule(new FluxParticleGravityModule());

            var plan = FluxParticleCompilePlan.Create(_authoring);
            var registry = new FluxParticleGpuModuleRegistry();
            var gpuParameters = new FluxParticleGpuParameterCompiler(registry).Compile(plan);

            ((JObject)gpuParameters["velocity"]).Add("_Unexpected", new JObject
            {
                ["type"] = (int)FluxParticleParameterType.Float,
                ["value"] = 1.0f
            });

            Assert.Throws<System.InvalidOperationException>(() => _compiler.Compile(plan, gpuParameters));
        }

        JObject Compile()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);
            var registry = new FluxParticleGpuModuleRegistry();
            var gpuParameters = new FluxParticleGpuParameterCompiler(registry).Compile(plan);

            return _compiler.Compile(plan, gpuParameters);
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