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
    public class FluxParticleGpuRuntimeDataWriterTests
    {
        GameObject _gameObject;
        FluxParticleAuthoring _authoring;
        FluxParticleGpuModuleRegistry _registry;
        FluxParticleGpuCompiler _compiler;
        FluxParticleGpuRuntimeDataWriter _writer;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("GPU Runtime Data Writer Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _registry = new FluxParticleGpuModuleRegistry();
            _compiler = new FluxParticleGpuCompiler(_registry);
            _writer = new FluxParticleGpuRuntimeDataWriter();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationMatchesLegacyOutput()
        {
            AssertMatchesLegacy();
        }

        [Test]
        public void StandardModulesMatchLegacyOutput()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleLimitVelocityModule());

            AssertMatchesLegacy();
        }

        [Test]
        public void DisabledModuleMatchesLegacyOutput()
        {
            AddModule(new FluxParticleGravityModule());

            var serializedObject = new SerializedObject(_authoring);
            var modules = serializedObject.FindProperty("modules");
            modules.GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssertMatchesLegacy();
        }

        [Test]
        public void EnabledParameterPreservesInitialState()
        {
            AddModule(new FluxParticleGravityModule());

            var compilation = Compile();
            var stage = compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var enabledName = stage.Modules[0].EnabledParameter.Name;
            var parameters = _writer.WriteParameters(compilation);

            Assert.AreEqual(
                1.0f,
                (float)parameters["velocity"][enabledName]["value"]);
        }

        [Test]
        public void ModuleBindingPreservesParameterOwnership()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleNoiseModule());

            var compilation = Compile();
            var bindings = _writer.WriteBindings(compilation);

            var gravity = bindings["tplab.fluxfx.gravity"];
            var noise = bindings["tplab.fluxfx.noise"];

            Assert.AreEqual((int)FluxParticleKernelTarget.Velocity, (int)gravity["target"]);
            Assert.AreEqual((int)FluxParticleParameterType.Vector, (int)gravity["parameters"]["_Gravity"]);
            Assert.IsNull(gravity["parameters"]["_NoiseStrength"]);

            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)noise["parameters"]["_NoiseStrength"]);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)noise["parameters"]["_NoiseScale"]);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)noise["parameters"]["_NoiseSpeed"]);
            Assert.IsNull(noise["parameters"]["_Gravity"]);
        }

        [Test]
        public void WritingMultipleTimesProducesEquivalentData()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleNoiseModule());

            var compilation = Compile();

            var firstParameters = _writer.WriteParameters(compilation);
            var secondParameters = _writer.WriteParameters(compilation);

            var firstBindings = _writer.WriteBindings(compilation);
            var secondBindings = _writer.WriteBindings(compilation);

            Assert.IsTrue(JToken.DeepEquals(firstParameters, secondParameters));
            Assert.IsTrue(JToken.DeepEquals(firstBindings, secondBindings));
        }

        void AssertMatchesLegacy()
        {
            var plan = FluxParticleCompilePlan.Create(_authoring);
            var compilation = Compile(plan);

            var legacyParameters = new FluxParticleGpuParameterCompiler(_registry).Compile(plan);
            var legacyBindings = new FluxParticleGpuModuleBindingCompiler(_registry).Compile(plan, legacyParameters);

            var parameters = _writer.WriteParameters(compilation);
            var bindings = _writer.WriteBindings(compilation);

            Assert.IsTrue(
                JToken.DeepEquals(legacyParameters, parameters),
                $"GPU parameter JSON mismatch.\nLegacy: {legacyParameters}\nNew: {parameters}");

            Assert.IsTrue(
                JToken.DeepEquals(legacyBindings, bindings),
                $"GPU module binding JSON mismatch.\nLegacy: {legacyBindings}\nNew: {bindings}");
        }

        FluxParticleGpuCompilation Compile()
        {
            return Compile(FluxParticleCompilePlan.Create(_authoring));
        }

        FluxParticleGpuCompilation Compile(FluxParticleCompilePlan plan)
        {
            var result = _compiler.Compile(plan);

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            return result.Compilation;
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