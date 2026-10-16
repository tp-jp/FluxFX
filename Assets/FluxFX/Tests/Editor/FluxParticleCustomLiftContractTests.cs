using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Modules;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CustomLiftContractPreservesEnabledAndSharesHandleValidation(bool enabled)
        {
            RequireGraphics();
            var definition = new FluxParticleModuleRegistry().GetAll().Single(item => item.ModuleId == "custom.fluxfx.lift");
            var module = (FluxParticleModule)Activator.CreateInstance(definition.ModuleType);
            typeof(FluxParticleModule).GetField("enabled", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(module, enabled);
            SetModules(module);
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var binding = compiled.ModuleBindings.Single(item => item.ModuleId == definition.ModuleId);
            var keys = definition.ModuleType.Assembly.GetType("TpLab.Flux.FX.CustomModules.CustomLiftParameter", true);
            Assert.AreEqual("custom-lift.strength", binding.Parameters[(int)Enum.Parse(keys, "Strength")].Id);
            Assert.AreEqual("custom-lift.direction", binding.Parameters[(int)Enum.Parse(keys, "Direction")].Id);
            Assert.IsTrue(binding.Parameters.All(parameter => parameter.Contract != null));
            var runtime = CreateRuntime(compiled);
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            var extensions = definition.ModuleType.Assembly.GetType("TpLab.Flux.FX.CustomModules.CustomLiftHandleExtensions", true);
            var handle = (FluxParticleModuleHandle)extensions.GetMethod("CustomLift").Invoke(null, new object[] { modules });
            Assert.AreSame(handle, extensions.GetMethod("CustomLift").Invoke(null, new object[] { modules }));
            Assert.AreEqual(enabled, handle.GetEnabled());
            Assert.IsTrue(runtime.Initialize(4));
            Assert.IsTrue(runtime.SetFloat("spawn.speed", 0));
            runtime.Step(0, 0, 1, 1);
            Assert.IsFalse(runtime.SetFloat("custom-lift.enabled", 0.5f));
            Assert.AreEqual(enabled, handle.GetEnabled());
            foreach (var value in new[] { -2f, 0f, 3f, float.NaN, float.PositiveInfinity })
            {
                var before = runtime.GetParameterValue(runtime.ResolveParameter("custom-lift.strength", 1));
                var expected = !float.IsNaN(value) && !float.IsInfinity(value);
                Assert.AreEqual(expected, extensions.GetMethod("SetStrength").Invoke(null, new object[] { handle, value }));
                Assert.AreEqual(expected, runtime.SetFloat("custom-lift.strength", value));
                if (!expected) Assert.AreEqual(before, runtime.GetParameterValue(runtime.ResolveParameter("custom-lift.strength", 1)));
            }
            Assert.IsFalse((bool)extensions.GetMethod("SetDirection").Invoke(null, new object[] { handle, new Vector3(0, float.NaN, 0) }));
            Assert.IsFalse(runtime.SetVector("custom-lift.direction", new Vector4(0, float.NaN, 0, 0)));
            Assert.IsTrue(handle.SetEnabled(false));
            runtime.Step(0.5f, 0, 0, 2);
            AssertVector(Vector4.zero, ReadValue(runtime, compiled, "particle.velocity", 0));
            Assert.IsTrue(handle.SetEnabled(true));
            runtime.Step(0.5f, 0, 0, 3);
            Assert.Greater(ReadValue(runtime, compiled, "particle.velocity", 0).y, 0);
        }

        [TestCase("strength")]
        [TestCase("direction")]
        public void CustomLiftContractRejectsInvalidAuthoringValues(string field)
        {
            var definition = new FluxParticleModuleRegistry().GetAll().Single(item => item.ModuleId == "custom.fluxfx.lift");
            var module = (FluxParticleModule)Activator.CreateInstance(definition.ModuleType);
            definition.ModuleType.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(module, field == "strength" ? (object)float.NaN : new Vector3(float.PositiveInfinity, 0, 0));
            SetModules(module);
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }
    }
}
