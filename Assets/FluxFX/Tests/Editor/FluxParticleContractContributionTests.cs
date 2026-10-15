using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Compilation.Contracts;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Editor.Contracts.Modules;
using TpLab.Flux.FX.Editor.Modules.Gravity;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using ContractPolicy = TpLab.Flux.FX.Editor.Contracts.FluxParticleParameterPolicy;
using CompiledPolicy = TpLab.Flux.FX.Editor.Compilation.Definitions.FluxParticleParameterPolicy;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleContractContributionTests
    {
        enum TimeMode { Simulation, ParticleAge }
        enum Inexact { Value = 16777217 }
        enum WideSigned : long { Minimum = long.MinValue, Exact = 1L << 60 }
        enum WideUnsigned : ulong { Exact = 1UL << 63, Inexact = ulong.MaxValue }

        [TestCase(true)]
        [TestCase(false)]
        public void GravityBuildPreservesIdentityInitialValuesAndRuntimeSlot(bool enabled)
        {
            var root = new GameObject("Gravity contract test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                var modules = serialized.FindProperty("modules");
                modules.arraySize = 1;
                modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleGravityModule();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                var gravity = serialized.FindProperty("modules").GetArrayElementAtIndex(0);
                gravity.FindPropertyRelative("enabled").boolValue = enabled;
                gravity.FindPropertyRelative("gravity").vector3Value = new Vector3(1, -2, 3);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var build = FluxParticleBuildPipeline.Build(authoring);
                var contract = FluxParticleGravityContract.Module;
                var parameter = build.Shaders.Compilation.Parameters["gravity"];
                var binding = build.ModuleBindings.Single(value => value.ModuleId == contract.Id);
                Assert.AreEqual("fluxfx.gravity", contract.Id);
                Assert.AreEqual("FluxParticleGravityHandle", contract.HandleName);
                Assert.AreEqual("Gravity", contract.AccessorName);
                Assert.AreEqual("Gravity", FluxParticleGravityContract.Acceleration.ApiName);
                Assert.AreEqual(FluxParticleValueType.Float3, parameter.Type);
                Assert.AreEqual(CompiledPolicy.Live, parameter.Policy);
                Assert.AreSame(FluxParticleGravityContract.Acceleration, parameter.Contract);
                Assert.AreSame(contract.Enabled, build.Shaders.Compilation.Parameters["gravity.enabled"].Contract);
                Assert.AreEqual(new Vector4(1, -2, 3, 0), build.InitialValues["gravity"]);
                Assert.AreEqual(enabled ? 1f : 0f, build.InitialValues["gravity.enabled"].x);
                Assert.AreEqual(1, binding.Parameters.Count);
                Assert.AreEqual("gravity", binding.Parameters[0].Id);
                Assert.AreEqual("gravity.enabled", binding.EnabledParameterId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void GravityRejectsInvalidAuthoringValuesBeforeRegisteringAcceleration(float value)
        {
            var settings = new FluxParticleGravityModule();
            typeof(FluxParticleGravityModule).GetField("gravity", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(settings, new Vector3(0, value, 0));
            var context = Context();
            var error = Assert.Throws<InvalidOperationException>(() => new FluxParticleGravityDefinition().Contribute(settings, context));
            StringAssert.Contains("gravity", error.Message);
            Assert.IsFalse(context.Parameters.ContainsKey("gravity"));
            Assert.IsEmpty(context.ModuleBindings);
        }

        [Test]
        public void ContractOrderDeterminesSlotsAndUnavailableParametersKeepTheirSlot()
        {
            var first = Float("test.first", "First");
            var second = Float("test.second", "Second");
            var third = Float("test.third", "Third");
            var module = Module().Parameter(first).Parameter(second).Parameter(third).Build();
            var context = Context();
            var contribution = new FluxParticleModuleContribution(context, module, true);
            contribution.Bind(third, 0.75f);
            contribution.Bind(first, 0.25f);
            Assert.Throws<InvalidOperationException>(() => contribution.Complete());
            contribution.UnavailableParameter(second);
            contribution.Complete();
            var parameters = context.ModuleBindings[module.Id].Parameters;
            Assert.AreEqual(first.Id, parameters[0].Id);
            Assert.IsNull(parameters[1]);
            Assert.AreEqual(third.Id, parameters[2].Id);
            Assert.IsFalse(context.Parameters.ContainsKey(second.Id));
            Assert.Throws<InvalidOperationException>(() => contribution.Bind(second, 2f));
        }

        [Test]
        public void BindingRequiresOwnedContractAndValidInitialValueWithoutConsumingSlotOnFailure()
        {
            var value = Float("test.value", "Value");
            var module = Module().Parameter(value).Build();
            var context = Context();
            var contribution = new FluxParticleModuleContribution(context, module, true);
            Assert.Throws<InvalidOperationException>(() => contribution.Bind(Float(value.Id, value.ApiName), 0.5f));
            Assert.Throws<InvalidOperationException>(() => contribution.Bind(value, 2f));
            Assert.IsFalse(context.Parameters.ContainsKey(value.Id));
            Assert.Throws<InvalidOperationException>(() => contribution.Parameter(0, value.Id, FluxParticleValueType.Float, CompiledPolicy.Live, Vector4.zero));
            Assert.Throws<InvalidOperationException>(() => contribution.UnavailableParameter(0));
            contribution.Bind(value, 0.5f);
            Assert.Throws<InvalidOperationException>(() => contribution.Bind(value, 0.75f));
            Assert.Throws<InvalidOperationException>(() => contribution.UnavailableParameter(value));
            contribution.Complete();
            Assert.AreEqual(0.5f, context.InitialValues[value.Id].x);
            Assert.AreSame(value, context.Parameters[value.Id].Contract);
        }

        [Test]
        public void EnabledIsOptionalButRequiredForDisabledOrSwitchableModules()
        {
            var module = Module().Build();
            Assert.Throws<InvalidOperationException>(() => new FluxParticleModuleContribution(Context(), module, false));
            var context = Context();
            var contribution = new FluxParticleModuleContribution(context, module, true);
            Assert.Throws<InvalidOperationException>(() => contribution.Operation("test.operation", FluxParticlePhase.Update,
                Array.Empty<FluxParticleValueRead>(), Array.Empty<FluxParticleValueWrite>(), "test.hlsl", "Test", true));
            contribution.Complete();
            Assert.IsNull(context.ModuleBindings[module.Id].EnabledParameterId);
            Assert.IsEmpty(context.Parameters);
        }

        [TestCase(ContractPolicy.Static, CompiledPolicy.Static)]
        [TestCase(ContractPolicy.Live, CompiledPolicy.Live)]
        [TestCase(ContractPolicy.SpawnLocked, CompiledPolicy.SpawnLocked)]
        public void CompilerPreservesPolicyAndSourceContract(ContractPolicy policy, CompiledPolicy expected)
        {
            var contract = FluxParticleParameter.Float("test.value").ApiName("Value").Policy(policy).Build();
            var compiled = FluxParticleParameterContractCompiler.Compile(contract);
            Assert.AreEqual(expected, compiled.Policy);
            Assert.AreSame(contract, compiled.Contract);
            Assert.IsNull(new FluxParticleParameterDefinition("legacy", FluxParticleValueType.Float, expected).Contract);
        }

        [Test]
        public void CompilerEncodesSemanticTypesWithoutChangingVectorComponentsOrHdrColors()
        {
            AssertEncoding(Float("float", "Float"), 0.5f, FluxParticleValueType.Float, new Vector4(0.5f, 0, 0, 0));
            AssertEncoding(FluxParticleParameter.Vector2("v2").ApiName("V2").Policy(ContractPolicy.Live).Build(),
                new Vector2(2, 3), FluxParticleValueType.Float2, new Vector4(2, 3, 0, 0));
            AssertEncoding(FluxParticleGravityContract.Acceleration,
                new Vector3(1, -2, 3), FluxParticleValueType.Float3, new Vector4(1, -2, 3, 0));
            AssertEncoding(FluxParticleParameter.Vector4("v4").ApiName("V4").Policy(ContractPolicy.Live).Build(),
                new Vector4(1, 2, 3, 4), FluxParticleValueType.Float4, new Vector4(1, 2, 3, 4));
            AssertEncoding(FluxParticleParameter.Color("color").ApiName("Color").Policy(ContractPolicy.Live).Build(),
                new Color(4, 2, 1, 0.5f), FluxParticleValueType.Float4, new Vector4(4, 2, 1, 0.5f));
            AssertEncoding(FluxParticleGravityContract.Module.Enabled, true, FluxParticleValueType.Float, new Vector4(1, 0, 0, 0));
            AssertEncoding(FluxParticleGravityContract.Module.Enabled, false, FluxParticleValueType.Float, Vector4.zero);
            var mode = FluxParticleParameter.Enum<TimeMode>("time.mode").ApiName("Mode").Policy(ContractPolicy.Live).Build();
            AssertEncoding(mode, TimeMode.ParticleAge, FluxParticleValueType.Float, new Vector4(1, 0, 0, 0));
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameterContractCompiler.Encode(mode, (TimeMode)99));
        }

        [Test]
        public void CompilerRejectsEnumRoundingAndAcceptsExactlyRepresentableWideValues()
        {
            var inexact = FluxParticleParameter.Enum<Inexact>("test.inexact").ApiName("Inexact").Policy(ContractPolicy.Live).Build();
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameterContractCompiler.Compile(inexact));
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameterContractCompiler.Encode(inexact, Inexact.Value));
            var signed = FluxParticleParameter.Enum<WideSigned>("test.signed").ApiName("Signed").Policy(ContractPolicy.Live).Build();
            AssertEncoding(signed, WideSigned.Minimum, FluxParticleValueType.Float, new Vector4((float)long.MinValue, 0, 0, 0));
            AssertEncoding(signed, WideSigned.Exact, FluxParticleValueType.Float, new Vector4((float)(1L << 60), 0, 0, 0));
            var unsignedBuilder = FluxParticleParameter.Enum<WideUnsigned>("test.unsigned").ApiName("Unsigned").Policy(ContractPolicy.Live);
            Assert.Throws<InvalidOperationException>(() => FluxParticleParameterContractCompiler.Compile(unsignedBuilder.Build()));
            AssertEncoding(unsignedBuilder.AllowedValues(WideUnsigned.Exact).Build(), WideUnsigned.Exact,
                FluxParticleValueType.Float, new Vector4((float)(1UL << 63), 0, 0, 0));
        }

        static void AssertEncoding<T>(FluxParticleParameterContract<T> contract, T value, FluxParticleValueType type, Vector4 expected) where T : struct
        {
            Assert.AreEqual(type, FluxParticleParameterContractCompiler.Compile(contract).Type);
            Assert.AreEqual(expected, FluxParticleParameterContractCompiler.Encode(contract, value));
        }

        static FluxParticleParameterContract<float> Float(string id, string apiName)
        {
            return FluxParticleParameter.Float(id).ApiName(apiName).Policy(ContractPolicy.Live).Range(0, 1).Build();
        }

        static FluxParticleModuleContractBuilder Module() => new FluxParticleModuleContractBuilder("test.module").Handle("TestHandle").Accessor("Test");

        static FluxParticleProgramContribution Context() => new FluxParticleProgramContribution(new FluxParticleDefinitionBuilder());
    }
}
