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
        FluxParticleGpuCompiler _compiler;
        FluxParticleGpuRuntimeDataWriter _writer;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("GPU Runtime Data Writer Test");
            _authoring = _gameObject.AddComponent<FluxParticleAuthoring>();
            _compiler = new FluxParticleGpuCompiler(new FluxParticleGpuModuleRegistry());
            _writer = new FluxParticleGpuRuntimeDataWriter();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EmptyConfigurationProducesEmptyData()
        {
            var compilation = Compile();
            var parameters = _writer.WriteParameters(compilation);
            var bindings = _writer.WriteBindings(compilation);

            Assert.IsNotNull(parameters["velocity"]);
            Assert.AreEqual(0, ((JObject)parameters["velocity"]).Count);
            Assert.AreEqual(0, bindings.Count);
        }

        [Test]
        public void GravityProducesVectorParameter()
        {
            AddModule(new FluxParticleGravityModule());

            var velocity = _writer.WriteParameters(Compile())["velocity"];
            var gravity = velocity["_Gravity"];

            Assert.IsNotNull(gravity);
            Assert.AreEqual((int)FluxParticleParameterType.Vector, (int)gravity["type"]);
            Assert.AreEqual(4, ((JArray)gravity["value"]).Count);

            Assert.AreEqual(0.0f, (float)gravity["value"][0]);
            Assert.AreEqual(-1.0f, (float)gravity["value"][1]);
            Assert.AreEqual(0.0f, (float)gravity["value"][2]);
        }

        [Test]
        public void DragProducesFloatParameter()
        {
            var module = new FluxParticleDragModule();
            AddModule(module);

            var velocity = _writer.WriteParameters(Compile())["velocity"];
            var drag = velocity["_Drag"];

            Assert.IsNotNull(drag);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)drag["type"]);
            Assert.AreEqual(module.Drag, (float)drag["value"]);
        }

        [Test]
        public void DisabledModulePreservesParametersAndBinding()
        {
            AddModule(new FluxParticleGravityModule());
            SetModuleEnabled(0, false);

            var compilation = Compile();
            var stage = compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var enabledName = stage.Modules[0].EnabledParameter.Name;

            var parameters = _writer.WriteParameters(compilation);
            var bindings = _writer.WriteBindings(compilation);

            Assert.IsNotNull(parameters["velocity"]["_Gravity"]);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)parameters["velocity"][enabledName]["type"]);
            Assert.AreEqual(0.0f, (float)parameters["velocity"][enabledName]["value"]);

            Assert.IsNotNull(bindings["tplab.fluxfx.gravity"]);
            Assert.AreEqual(enabledName, (string)bindings["tplab.fluxfx.gravity"]["enabledParameter"]);
        }

        [Test]
        public void EnabledParameterPreservesInitialState()
        {
            AddModule(new FluxParticleGravityModule());

            var compilation = Compile();
            var stage = compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);
            var enabledName = stage.Modules[0].EnabledParameter.Name;

            var parameters = _writer.WriteParameters(compilation);

            Assert.AreEqual(1.0f, (float)parameters["velocity"][enabledName]["value"]);
        }

        [Test]
        public void MultipleModulesProduceIndependentParameters()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            var compilation = Compile();
            var velocity = (JObject)_writer.WriteParameters(compilation)["velocity"];

            Assert.AreEqual(8, velocity.Count);
            Assert.IsNotNull(velocity["_Gravity"]);
            Assert.IsNotNull(velocity["_Force"]);
            Assert.IsNotNull(velocity["_Drag"]);
            Assert.IsNotNull(velocity["_MaxSpeed"]);

            var stage = compilation.GetStage(FluxParticleExecutionStage.VelocityUpdate);

            foreach (var module in stage.Modules)
            {
                var enabledName = module.EnabledParameter.Name;

                Assert.IsNotNull(velocity[enabledName], $"Enabled parameter missing: {module.ModuleId}");
                Assert.AreEqual((int)FluxParticleParameterType.Float, (int)velocity[enabledName]["type"]);
            }
        }

        [Test]
        public void StandardModulesProduceExpectedParameterTypes()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            var velocity = _writer.WriteParameters(Compile())["velocity"];

            AssertParameterType(velocity, "_Gravity", FluxParticleParameterType.Vector);
            AssertParameterType(velocity, "_Force", FluxParticleParameterType.Vector);
            AssertParameterType(velocity, "_NoiseStrength", FluxParticleParameterType.Float);
            AssertParameterType(velocity, "_NoiseScale", FluxParticleParameterType.Float);
            AssertParameterType(velocity, "_NoiseSpeed", FluxParticleParameterType.Float);
            AssertParameterType(velocity, "_VortexCenter", FluxParticleParameterType.Vector);
            AssertParameterType(velocity, "_VortexAxis", FluxParticleParameterType.Vector);
            AssertParameterType(velocity, "_VortexStrength", FluxParticleParameterType.Float);
            AssertParameterType(velocity, "_Drag", FluxParticleParameterType.Float);
            AssertParameterType(velocity, "_MaxSpeed", FluxParticleParameterType.Float);
        }

        [Test]
        public void ModuleBindingPreservesParameterOwnership()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleNoiseModule());

            var bindings = _writer.WriteBindings(Compile());

            var gravity = bindings["tplab.fluxfx.gravity"];
            var noise = bindings["tplab.fluxfx.noise"];

            Assert.AreEqual(2, bindings.Count);

            Assert.AreEqual((int)FluxParticleKernelTarget.Velocity, (int)gravity["target"]);
            Assert.AreEqual((int)FluxParticleParameterType.Vector, (int)gravity["parameters"]["_Gravity"]);
            Assert.AreEqual(1, ((JObject)gravity["parameters"]).Count);
            Assert.IsNull(gravity["parameters"]["_NoiseStrength"]);

            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)noise["parameters"]["_NoiseStrength"]);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)noise["parameters"]["_NoiseScale"]);
            Assert.AreEqual((int)FluxParticleParameterType.Float, (int)noise["parameters"]["_NoiseSpeed"]);
            Assert.IsNull(noise["parameters"]["_Gravity"]);
        }

        [Test]
        public void EveryBindingReferencesExistingParametersWithMatchingTypes()
        {
            AddModule(new FluxParticleGravityModule());
            AddModule(new FluxParticleForceModule());
            AddModule(new FluxParticleNoiseModule());
            AddModule(new FluxParticleVortexModule());
            AddModule(new FluxParticleDragModule());
            AddModule(new FluxParticleLimitVelocityModule());

            var compilation = Compile();
            var velocity = (JObject)_writer.WriteParameters(compilation)["velocity"];
            var bindings = _writer.WriteBindings(compilation);
            var ownedNames = new System.Collections.Generic.HashSet<string>();

            foreach (var bindingProperty in bindings.Properties())
            {
                var binding = bindingProperty.Value;
                var enabledName = (string)binding["enabledParameter"];

                Assert.IsTrue(ownedNames.Add(enabledName), $"Duplicate enabled parameter: {enabledName}");
                AssertParameterType(velocity, enabledName, FluxParticleParameterType.Float);

                foreach (var parameter in ((JObject)binding["parameters"]).Properties())
                {
                    Assert.IsTrue(ownedNames.Add(parameter.Name), $"Duplicate parameter ownership: {parameter.Name}");
                    Assert.IsNotNull(velocity[parameter.Name], $"Missing parameter: {parameter.Name}");
                    Assert.AreEqual((int)parameter.Value, (int)velocity[parameter.Name]["type"]);
                }
            }

            Assert.AreEqual(velocity.Count, ownedNames.Count, "Unowned GPU parameters were found.");
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

        [Test]
        public void EmptyConfigurationProducesBothStageParameters()
        {
            var parameters = _writer.WriteParameters(Compile());

            Assert.IsNotNull(parameters["velocity"]);
            Assert.IsNotNull(parameters["position"]);
            Assert.AreEqual(0, ((JObject)parameters["velocity"]).Count);
            Assert.AreEqual(0, ((JObject)parameters["position"]).Count);
        }

        [Test]
        public void PositionModuleProducesParametersAndBinding()
        {
            AddModule(new TestPositionGpuModule());

            var compilation = Compile();
            var parameters = _writer.WriteParameters(compilation);
            var bindings = _writer.WriteBindings(compilation);

            var position = parameters["position"];
            var binding = bindings["test.fluxfx.position"];
            var stage = compilation.GetStage(FluxParticleExecutionStage.PositionUpdate);
            var enabledName = stage.Modules[0].EnabledParameter.Name;

            Assert.IsNotNull(position["_TestPositionStrength"]);
            Assert.IsNotNull(position[enabledName]);
            Assert.IsNotNull(binding);

            Assert.AreEqual(
                (int)FluxParticleKernelTarget.Position,
                (int)binding["target"]);

            Assert.AreEqual(
                enabledName,
                (string)binding["enabledParameter"]);
        }

        [Test]
        public void SameParameterNameAcrossStagesProducesIndependentData()
        {
            TestGpuModuleDefinition.ParameterName = "_SharedStrength";
            TestPositionGpuModuleDefinition.ParameterName = "_SharedStrength";

            try
            {
                AddModule(new TestGpuModule());
                AddModule(new TestPositionGpuModule());

                var compilation = Compile();
                var parameters = _writer.WriteParameters(compilation);

                Assert.IsNotNull(parameters["velocity"]["_SharedStrength"]);
                Assert.IsNotNull(parameters["position"]["_SharedStrength"]);
            }
            finally
            {
                TestGpuModuleDefinition.ParameterName = "_TestStrength";
                TestPositionGpuModuleDefinition.ParameterName = "_TestPositionStrength";
            }
        }

        static void AssertParameterType(JToken velocity, string name, FluxParticleParameterType type)
        {
            var parameter = velocity[name];

            Assert.IsNotNull(parameter, $"Missing GPU parameter: {name}");
            Assert.AreEqual((int)type, (int)parameter["type"]);
        }

        FluxParticleGpuCompilation Compile()
        {
            var result = _compiler.Compile(FluxParticleCompilePlan.Create(_authoring));

            Assert.IsTrue(result.Success, result.Validation.GetReport());

            return result.Compilation;
        }

        void SetModuleEnabled(int index, bool enabled)
        {
            var serializedObject = new SerializedObject(_authoring);
            serializedObject.FindProperty("modules")
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("enabled").boolValue = enabled;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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