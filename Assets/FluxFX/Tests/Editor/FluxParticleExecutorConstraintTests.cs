using System;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Compilation.Contracts;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void GravityAndSizeShareValidationAcrossHandleAndExecutor(bool initialized)
        {
            RequireGraphics();
            SetModules(CreateStandardModules());
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            if (initialized) Assert.IsTrue(runtime.Initialize(4));
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            var sizeSlot = runtime.ResolveParameter("size.end", (int)FluxParticleValueType.Float);
            var gravitySlot = runtime.ResolveParameter("gravity", (int)FluxParticleValueType.Float3);
            foreach (var value in new[] { 0f, 2f, float.MaxValue, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var valid = value >= 0 && !float.IsInfinity(value);
                Assert.IsTrue(modules.SizeOverLifetime.SetEndSize(2));
                Assert.AreEqual(valid, modules.SizeOverLifetime.SetEndSize(value), "Size handle " + value);
                Assert.AreEqual(valid ? value : 2, modules.SizeOverLifetime.GetEndSize());
                Assert.IsTrue(modules.SizeOverLifetime.SetEndSize(2));
                Assert.AreEqual(valid, runtime.SetFloat("size.end", value), "Size direct " + value);
                Assert.AreEqual(valid ? value : 2, modules.SizeOverLifetime.GetEndSize());
                Assert.IsTrue(modules.SizeOverLifetime.SetEndSize(2));
                Assert.AreEqual(valid, runtime.CanSetParameterValue(sizeSlot, 1, new Vector4(value, 0, 0, 0)));
                Assert.AreEqual(valid, runtime.SetParameterValue(sizeSlot, 1, new Vector4(value, 0, 0, 0)));
                Assert.AreEqual(valid ? value : 2, modules.SizeOverLifetime.GetEndSize());
            }
            for (var component = 0; component < 3; component++)
            {
                foreach (var value in new[] { -9.81f, 0f, float.MaxValue, -float.MaxValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                {
                    var vector = Vector3.zero;
                    vector[component] = value;
                    var valid = !float.IsNaN(value) && !float.IsInfinity(value);
                    Assert.IsTrue(modules.Gravity.SetGravity(Vector3.one));
                    Assert.AreEqual(valid, modules.Gravity.SetGravity(vector));
                    Assert.AreEqual(valid ? vector : Vector3.one, modules.Gravity.GetGravity());
                    Assert.IsTrue(modules.Gravity.SetGravity(Vector3.one));
                    Assert.AreEqual(valid, runtime.SetVector("gravity", vector));
                    Assert.AreEqual(valid ? vector : Vector3.one, modules.Gravity.GetGravity());
                    Assert.IsTrue(modules.Gravity.SetGravity(Vector3.one));
                    Assert.AreEqual(valid, runtime.SetParameterValue(gravitySlot, 3, vector));
                    Assert.AreEqual(valid ? vector : Vector3.one, modules.Gravity.GetGravity());
                }
            }
            Assert.IsFalse(runtime.SetFloat("gravity", 1));
            Assert.IsFalse(runtime.SetVector("size.end", Vector4.one));
        }

        [Test]
        public void RuntimeNumericConstraintsPreserveComponentBoundsAndIntegerRules()
        {
            RequireGraphics();
            SetModules(CreateStandardModules());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            var parameters = compiled.Shaders.Compilation.Parameters.Values.OrderBy(parameter => parameter.Id, StringComparer.Ordinal).ToArray();
            var slot = runtime.ResolveParameter("gravity", 3);
            var contract = FluxParticleParameter.Vector3("gravity").ApiName("Gravity")
                .Policy(TpLab.Flux.FX.Editor.Contracts.FluxParticleParameterPolicy.Live)
                .Minimum(new Vector3(-2, -3, -4), false).Maximum(new Vector3(2, 3, 4), false).Integer().Build();
            parameters[slot] = FluxParticleParameterContractCompiler.Compile(contract);
            var constraints = new FluxParticleRuntimeParameterConstraints(parameters, new[] { contract });
            runtime.SetProgramVariable("parameterConstraintFlags", constraints.Flags);
            runtime.SetProgramVariable("parameterMinimum", constraints.Minimum);
            runtime.SetProgramVariable("parameterMaximum", constraints.Maximum);
            var values = new[]
            {
                Vector3.zero, new Vector3(-1, 2, 3), new Vector3(-2, 0, 0), new Vector3(0, -3, 0),
                new Vector3(0, 0, -4), new Vector3(2, 0, 0), new Vector3(0, 3, 0), new Vector3(0, 0, 4),
                new Vector3(0.5f, 0, 0), new Vector3(0, -0.5f, 0), new Vector3(0, 0, 1.5f),
            };
            foreach (var value in values)
            {
                Assert.IsTrue(modules.Gravity.SetGravity(Vector3.zero));
                Assert.AreEqual(contract.IsValid(value), modules.Gravity.SetGravity(value));
                Assert.AreEqual(contract.IsValid(value) ? value : Vector3.zero, modules.Gravity.GetGravity());
                Assert.IsTrue(modules.Gravity.SetGravity(Vector3.zero));
                Assert.AreEqual(contract.IsValid(value), runtime.SetVector("gravity", value));
                Assert.AreEqual(contract.IsValid(value) ? value : Vector3.zero, modules.Gravity.GetGravity());
            }
        }

        [Test]
        public void MissingRuntimeConstraintManifestRejectsUpdates()
        {
            RequireGraphics();
            SetModules(CreateStandardModules());
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            var size = runtime.GetComponentInParent<FluxParticleSystem>().Modules.SizeOverLifetime;
            var previous = size.GetEndSize();
            runtime.SetProgramVariable("parameterConstraintFlags", new int[0]);
            Assert.IsFalse(size.SetEndSize(3));
            Assert.IsFalse(runtime.SetFloat("size.end", 3));
            Assert.AreEqual(previous, size.GetEndSize());
        }
    }
}
