using NUnit.Framework;
using System;
using System.Linq;
using TpLab.Flux.FX.Editor.Modules;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using UnityEditor;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [Test]
        public void LiveInspectorModuleBindingsCoverLiveContractsAndExistingFields()
        {
            var bindings = TypeCache.GetTypesDerivedFrom<FluxParticleLiveInspectorProvider>()
                .Where(type => !type.IsAbstract)
                .SelectMany(type => ((FluxParticleLiveInspectorProvider)Activator.CreateInstance(type)).Bindings)
                .Where(binding => binding.ModuleType != null).ToArray();
            var types = bindings.Select(binding => binding.ModuleType).Distinct().ToArray();
            SetModules(types.Select(type => (FluxParticleModule)Activator.CreateInstance(type)).ToArray());
            var properties = new SerializedObject(authoring).FindProperty("modules");
            foreach (var binding in bindings)
            {
                var index = Array.IndexOf(types, binding.ModuleType);
                Assert.IsNotNull(properties.GetArrayElementAtIndex(index).FindPropertyRelative(binding.PropertyPath),
                    binding.ModuleType.Name + "." + binding.PropertyPath);
            }
            var build = FluxParticleBuildPipeline.Build(authoring);
            foreach (var parameter in build.Shaders.Compilation.Parameters.Values)
            {
                if (parameter.Policy != FluxParticleParameterPolicy.Live || parameter.Id.StartsWith("spawn.") ||
                    parameter.Id.StartsWith("shape.") || parameter.Id == "render.intensity") continue;
                Assert.IsTrue(bindings.Any(binding => binding.Contract.Id == parameter.Id), parameter.Id);
            }
        }

        [Test]
        public void LiveInspectorColorAndEnumChangesUseRuntimeConstraints()
        {
            RequireGraphics();
            SetModules(new FluxParticleBlinkModule(), new FluxParticleColorOverLifetimeModule());
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            Assert.IsTrue(runtime.Initialize(4));
            var color = new Vector4(2, 0.5f, 0.25f, 1);
            Assert.IsTrue(InspectorRequest(runtime, "color.end", 4, true, ref color));
            Assert.AreEqual(color, runtime.GetParameterValue(runtime.ResolveParameter("color.end", 4)));
            var value = new Vector4((float)(int)FluxParticleBlinkTarget.Alpha, 0, 0, 0);
            Assert.IsTrue(InspectorRequest(runtime, "blink.target", 1, true, ref value));
            value.x = 99;
            Assert.IsFalse(InspectorRequest(runtime, "blink.target", 1, true, ref value));
            Assert.AreEqual((float)(int)FluxParticleBlinkTarget.Alpha, value.x);
        }

        [Test]
        public void LiveInspectorRateValidatesAndReadsExternalChanges()
        {
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            var emitter = runtime.GetComponentInParent<FluxParticleSystem>().Emitter;
            typeof(FluxParticleEmitter).GetMethod("Initialize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(emitter, new object[] { 4 });
            emitter.SetProgramVariable("_inspectorParameterId", "emission.rate");
            emitter.SetProgramVariable("_inspectorParameterType", 1);
            foreach (var rate in new[] { 0f, 8f, -1f, float.NaN, float.PositiveInfinity })
            {
                var previous = emitter.Rate;
                emitter.SetProgramVariable("_inspectorWrite", true);
                emitter.SetProgramVariable("_inspectorValue", new Vector4(rate, 0, 0, 0));
                emitter._ApplyInspectorRequest();
                var valid = rate >= 0 && !float.IsInfinity(rate);
                Assert.AreEqual(valid, emitter.GetProgramVariable("_inspectorAccepted"));
                Assert.AreEqual(valid ? rate : previous, emitter.Rate);
            }
            Assert.IsTrue(emitter.SetRate(12));
            emitter.SetProgramVariable("_inspectorWrite", false);
            emitter._ApplyInspectorRequest();
            Assert.AreEqual(12, ((Vector4)emitter.GetProgramVariable("_inspectorValue")).x);
        }

        [Test]
        public void LiveInspectorSpeedRangeRejectsReversedEndpoints()
        {
            RequireGraphics();
            var settings = new SerializedObject(authoring);
            settings.FindProperty("initialVelocity.mode").intValue = (int)TpLab.Flux.FX.Scripts.FluxParticleStartSpeedMode.RandomBetweenTwoConstants;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            Assert.IsTrue(runtime.Initialize(4));
            var value = new Vector4(2, 4, 0, 0);
            Assert.IsTrue(InspectorRequest(runtime, "spawn.speed-range", 2, true, ref value));
            value.x = 5;
            Assert.IsFalse(InspectorRequest(runtime, "spawn.speed-range", 2, true, ref value));
            Assert.AreEqual(new Vector4(2, 4, 0, 0), value);
            Assert.IsFalse(InspectorRequest(runtime, "spawn.speed", 1, true, ref value), "Constant mode is not built");
        }

        static bool InspectorRequest(FluxParticleExecutor runtime, string id, int type, bool write, ref Vector4 value)
        {
            runtime.SetProgramVariable("_inspectorParameterId", id);
            runtime.SetProgramVariable("_inspectorParameterType", type);
            runtime.SetProgramVariable("_inspectorValue", value);
            runtime.SetProgramVariable("_inspectorWrite", write);
            runtime._ApplyInspectorRequest();
            value = (Vector4)runtime.GetProgramVariable("_inspectorValue");
            return (bool)runtime.GetProgramVariable("_inspectorAccepted");
        }

        [TestCase("gravity", 3)]
        [TestCase("render.intensity", 1)]
        [TestCase("custom-lift.strength", 1)]
        [TestCase("custom-lift.direction", 3)]
        public void LiveInspectorUsesExecutorValidationAndReadsExternalChanges(string id, int type)
        {
            RequireGraphics();
            var definition = new FluxParticleModuleRegistry().GetAll().Single(item => item.ModuleId == "custom.fluxfx.lift");
            SetModules(new FluxParticleGravityModule(), (FluxParticleModule)Activator.CreateInstance(definition.ModuleType));
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            var value = Vector4.one;
            Assert.IsFalse(InspectorRequest(runtime, id, type, true, ref value), "Uninitialized");
            Assert.IsTrue(runtime.Initialize(4));
            value = new Vector4(2, 0, 0, 0);
            Assert.IsTrue(InspectorRequest(runtime, id, type, true, ref value));
            var slot = runtime.ResolveParameter(id, type);
            Assert.AreEqual(value, runtime.GetParameterValue(slot));
            var invalid = new Vector4(float.NaN, 0, 0, 0);
            Assert.IsFalse(InspectorRequest(runtime, id, type, true, ref invalid));
            Assert.AreEqual(value, invalid, "Rejected input must return the current value");
            Assert.IsTrue(runtime.SetParameterValue(slot, type, new Vector4(3, 0, 0, 0)));
            Assert.IsTrue(InspectorRequest(runtime, id, type, false, ref value));
            Assert.AreEqual(3, value.x);
            var missing = value;
            Assert.IsFalse(InspectorRequest(runtime, "missing", type, true, ref missing));
            Assert.IsFalse(InspectorRequest(runtime, "render.size", 1, true, ref missing), "Static parameter");
            Assert.IsFalse(InspectorRequest(runtime, id, type == 1 ? 3 : 1, true, ref missing), "Wrong type");
            if (id != "render.intensity") return;
            value.x = -1;
            Assert.IsFalse(InspectorRequest(runtime, id, type, true, ref value));
            Assert.AreEqual(3, value.x);
        }

        [Test]
        public void LiveInspectorDurationClearsHistoryOnlyOnAcceptedChange()
        {
            RequireGraphics();
            SetModules(CreateTrails(3, 8, 0.5f));
            var build = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 4, 0);
            runtime.Step(0.1f, 0, 0, 0);
            var previous = ReadTexture(runtime.GetTrailPoint(0), 0);
            var value = Vector4.zero;
            Assert.IsFalse(InspectorRequest(runtime, "trails.duration", 1, true, ref value));
            Assert.AreEqual(previous, ReadTexture(runtime.GetTrailPoint(0), 0));
            value.x = 0.5f;
            Assert.IsTrue(InspectorRequest(runtime, "trails.duration", 1, true, ref value));
            Assert.AreEqual(previous, ReadTexture(runtime.GetTrailPoint(0), 0));
            var age = ReadValue(runtime, build, "particle.age", 0);
            value.x = 1;
            Assert.IsTrue(InspectorRequest(runtime, "trails.duration", 1, true, ref value));
            AssertVector(age, ReadValue(runtime, build, "particle.age", 0));
            for (var i = 0; i < 8; i++)
            {
                Assert.AreEqual(-1, ReadTexture(runtime.GetTrailPoint(i), 0).a);
            }
        }
    }
}
