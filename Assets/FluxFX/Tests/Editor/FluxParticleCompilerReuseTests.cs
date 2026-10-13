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
    public class FluxParticleCompilerReuseTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Particle Compiler Reuse Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleCompiler();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationProducesEquivalentJson()
        {
            AssertEquivalentJson();
        }

        [Test]
        public void StandardModulesProduceEquivalentJson()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleDragModule());

            AssertEquivalentJson();
        }

        [Test]
        public void DisabledModuleProducesEquivalentJson()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssertEquivalentJson();
        }

        [Test]
        public void ReusedCompilationPreservesGpuParameterSnapshot()
        {
            AddModule(new FluxParticleGravityModule());

            var plan = FluxParticleCompilePlan.Create(_authoring);
            var result = new FluxParticleGpuCompiler(new FluxParticleGpuModuleRegistry()).Compile(plan);

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            var first = JObject.Parse(_compiler.Compile(_authoring, plan, result.Compilation));

            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("gravity").vector3Value = new Vector3(1, -5, 2);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var second = JObject.Parse(_compiler.Compile(_authoring, plan, result.Compilation));

            Assert.IsTrue(JToken.DeepEquals(first["gpuParameters"], second["gpuParameters"]));
            Assert.IsTrue(JToken.DeepEquals(first["gpuModuleBindings"], second["gpuModuleBindings"]));
        }

        [Test]
        public void VelocityOverLifetimeUsesPositionGpuParametersOnly()
        {
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            var json = JObject.Parse(_compiler.Compile(_authoring));

            Assert.IsNull(json["velocityOverLifetime"]);

            var position = json["gpuParameters"]["position"];
            var binding = json["gpuModuleBindings"]["tplab.fluxfx.velocity-over-lifetime"];

            Assert.IsNotNull(position["_VelocityOverLifetimeStart"]);
            Assert.IsNotNull(position["_VelocityOverLifetimeEnd"]);
            Assert.IsNotNull(position["_VelocityOverLifetimeOrbital"]);
            Assert.IsNotNull(position["_VelocityOverLifetimeOffset"]);
            Assert.IsNotNull(position["_VelocityOverLifetimeRadial"]);
            Assert.IsNotNull(binding);

            Assert.AreEqual(
                (int)TpLab.Flux.FX.Udon.FluxParticleKernelTarget.Position,
                (int)binding["target"]);

            var enabledParameter = (string)binding["enabledParameter"];

            Assert.IsNotNull(position[enabledParameter]);
        }

        [Test]
        public void VelocityOverLifetimeProducesEquivalentJson()
        {
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            AssertEquivalentJson();
        }

        [Test]
        public void VelocityOverLifetimeAndVelocityModulesProduceEquivalentJson()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleVelocityOverLifetimeModule());

            AssertEquivalentJson();
        }

        void AssertEquivalentJson()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);
            var result = new FluxParticleGpuCompiler(new FluxParticleGpuModuleRegistry()).Compile(plan);

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            var normal = JObject.Parse(_compiler.Compile(_authoring));
            var reused = JObject.Parse(_compiler.Compile(_authoring, plan, result.Compilation));

            Assert.IsTrue(JToken.DeepEquals(normal, reused), $"Runtime JSON mismatch.\nNormal: {normal}\nReused: {reused}");
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