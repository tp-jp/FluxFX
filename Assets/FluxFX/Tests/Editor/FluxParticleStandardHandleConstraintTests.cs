using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Editor.Compilation.Contracts;
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
        public void AllStandardHandlesAndDirectUpdatesShareConstraints(bool initialized)
        {
            RequireGraphics();
            SetModules(CreateStandardModules().Concat(new FluxParticleModule[] { new FluxParticleBlinkModule(), new FluxParticleTrailsModule() }).ToArray());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            if (initialized) Assert.IsTrue(runtime.Initialize(4));
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            foreach (var binding in compiled.ModuleBindings)
            {
                var contractType = typeof(FluxParticleBuildPipeline).Assembly.GetTypes()
                    .Where(type => type.IsAbstract && type.IsSealed && type.Name.StartsWith("FluxParticle") && type.Name.EndsWith("Contract"))
                    .Single(type => type.GetProperty("Module")?.GetValue(null) is FluxParticleModuleContract candidate && candidate.Id == binding.ModuleId);
                var moduleContract = (FluxParticleModuleContract)contractType.GetProperty("Module").GetValue(null);
                var name = moduleContract.AccessorName;
                var handle = typeof(FluxParticleModules).GetProperty(name).GetValue(modules);
                Assert.IsNotNull(handle, name);
                var extensions = typeof(FluxParticleModules).Assembly.GetType("TpLab.Flux.FX.Udon.Modules.FluxParticle" + name + "HandleExtensions", true);
                foreach (var parameter in binding.Parameters)
                {
                    if (parameter == null) continue;
                    var contract = parameter.Contract;
                    var setter = extensions.GetMethod("Set" + contract.ApiName);
                    Assert.IsNotNull(setter, contract.Id);
                    var slot = runtime.ResolveParameter(parameter.Id, (int)parameter.Type);
                    var initial = runtime.GetParameterValue(slot);
                    foreach (var value in ConstraintCandidates(contract))
                    {
                        var arguments = HandleArguments(handle, contract, value);
                        if (arguments == null) continue;
                        Assert.IsTrue(runtime.SetParameterValue(slot, (int)parameter.Type, initial));
                        var byHandle = (bool)setter.Invoke(null, arguments);
                        var expected = byHandle ? value : initial;
                        Assert.AreEqual(expected, runtime.GetParameterValue(slot), parameter.Id + " handle");
                        Assert.IsTrue(runtime.SetParameterValue(slot, (int)parameter.Type, initial));
                        var byId = (int)parameter.Type == 1 ? runtime.SetFloat(parameter.Id, value.x) : runtime.SetVector(parameter.Id, value);
                        Assert.AreEqual(byHandle, byId, parameter.Id + " ID " + value);
                        Assert.AreEqual(expected, runtime.GetParameterValue(slot), parameter.Id + " ID value");
                        Assert.IsTrue(runtime.SetParameterValue(slot, (int)parameter.Type, initial));
                        Assert.AreEqual(byHandle, runtime.CanSetParameterValue(slot, (int)parameter.Type, value), parameter.Id + " validation");
                        Assert.AreEqual(byHandle, runtime.SetParameterValue(slot, (int)parameter.Type, value), parameter.Id + " slot");
                        Assert.AreEqual(expected, runtime.GetParameterValue(slot), parameter.Id + " slot value");
                    }
                    Assert.IsTrue(runtime.SetParameterValue(slot, (int)parameter.Type, initial));
                }
                Assert.IsFalse(runtime.SetFloat(binding.EnabledParameterId, 0.5f));
                Assert.IsFalse(runtime.SetFloat(binding.EnabledParameterId, 2));
                Assert.IsTrue(runtime.SetFloat(binding.EnabledParameterId, 0));
                Assert.IsTrue(runtime.SetFloat(binding.EnabledParameterId, 1));
            }
            Assert.IsTrue(modules.Drag.SetDrag(-1));
            Assert.IsTrue(runtime.SetFloat("drag", -1));
            Assert.IsFalse(runtime.SetFloat("blink.target", 0.5f));
            Assert.IsFalse(runtime.SetFloat("blink.time-mode", 0.5f));
            Assert.IsFalse(runtime.SetVector("texture-sheet.tiles", new Vector4(1.5f, 1, 0, 0)));
            Assert.IsFalse(modules.TextureSheetAnimation.SetTiles(16777217, 1));
            if (!initialized)
            {
                Assert.IsTrue(modules.TextureSheetAnimation.SetTiles(4095, 4097));
                Assert.IsTrue(runtime.SetVector("texture-sheet.tiles", new Vector4(4095, 4097, 0, 0)));
                Assert.IsFalse(modules.TextureSheetAnimation.SetTiles(97, 172961));
                Assert.IsFalse(runtime.SetVector("texture-sheet.tiles", new Vector4(97, 172961, 0, 0)));
                Assert.AreEqual(new Vector2(4095, 4097), modules.TextureSheetAnimation.GetTiles());
            }
        }

        static IEnumerable<Vector4> ConstraintCandidates(FluxParticleParameterContract contract)
        {
            if (contract.Kind == FluxParticleParameterKind.Enum)
            {
                foreach (var value in new[] { -1f, 0f, 1f, 2f, 99f })
                {
                    yield return new Vector4(value, 0, 0, 0);
                }
                yield break;
            }
            var count = contract.NumericConstraint.ComponentCount;
            foreach (var value in new[] { -1f, 0f, 0.5f, 1f, 2f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                for (var component = 0; component < count; component++)
                {
                    var vector = Vector4.zero;
                    for (var i = 0; i < count; i++)
                    {
                        vector[i] = 1;
                    }
                    vector[component] = value;
                    yield return vector;
                }
            }
        }

        static object[] HandleArguments(object handle, FluxParticleParameterContract contract, Vector4 value)
        {
            switch (contract.Kind)
            {
                case FluxParticleParameterKind.Float: return new object[] { handle, value.x };
                case FluxParticleParameterKind.Vector3: return new object[] { handle, (Vector3)value };
                case FluxParticleParameterKind.Color: return new object[] { handle, (Color)value };
                case FluxParticleParameterKind.Enum: return new object[] { handle, Enum.ToObject(contract.ValueType, (int)value.x) };
                case FluxParticleParameterKind.Vector2:
                    if (float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                        value.x != Mathf.Floor(value.x) || value.y != Mathf.Floor(value.y)) return null;
                    return new object[] { handle, (int)value.x, (int)value.y };
                default: throw new InvalidOperationException(contract.Id);
            }
        }

        [Test]
        public void RuntimeAllowedValuesPreserveSparseEnumContract()
        {
            RequireGraphics();
            SetModules(CreateStandardModules().Concat(new FluxParticleModule[] { new FluxParticleBlinkModule() }).ToArray());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            var parameters = compiled.Shaders.Compilation.Parameters.Values.OrderBy(parameter => parameter.Id, StringComparer.Ordinal).ToArray();
            var slot = runtime.ResolveParameter("blink.target", 1);
            var contract = FluxParticleParameter.Enum<FluxParticleBlinkTarget>("blink.target").ApiName("Target")
                .Policy(TpLab.Flux.FX.Editor.Contracts.FluxParticleParameterPolicy.Live)
                .AllowedValues(FluxParticleBlinkTarget.Rgb, FluxParticleBlinkTarget.Both).Build();
            parameters[slot] = FluxParticleParameterContractCompiler.Compile(contract);
            var constraints = new FluxParticleRuntimeParameterConstraints(parameters);
            runtime.SetProgramVariable("parameterConstraintFlags", constraints.Flags);
            runtime.SetProgramVariable("parameterAllowedValueOffsets", constraints.AllowedValueOffsets);
            runtime.SetProgramVariable("parameterAllowedValues", constraints.AllowedValues);
            var handle = runtime.GetComponentInParent<FluxParticleSystem>().Modules.Blink;
            Assert.IsTrue(handle.SetTarget(FluxParticleBlinkTarget.Both));
            Assert.IsFalse(handle.SetTarget(FluxParticleBlinkTarget.Alpha));
            Assert.IsFalse(runtime.SetFloat("blink.target", 1));
            Assert.AreEqual(FluxParticleBlinkTarget.Both, handle.GetTarget());
            Assert.IsTrue(runtime.SetFloat("blink.target", 0));
        }
    }
}
