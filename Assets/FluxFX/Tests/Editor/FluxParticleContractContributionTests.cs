using TpLab.Flux.FX.Editor.Modules.SizeOverLifetime;
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
using TpLab.Flux.FX.Editor.Modules.ColorOverLifetime;
using TpLab.Flux.FX.Editor.Modules.RotationOverLifetime;
using TpLab.Flux.FX.Editor.Preview;
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
        enum ParameterKey { First, Second, Third }
        enum OtherKey { First }
        enum AliasedKey { First, Second = First }
        enum SparseKey { First, Second = 2 }
        enum NegativeKey { First = -1 }
        enum WideKey : ulong { First = ulong.MaxValue }

        [Test]
        public void EnumKeysDetermineBindingOrderAndPreserveUnavailableSlots()
        {
            var first = Float("test.first", "First");
            var second = Float("test.second", "Second");
            var third = Float("test.third", "Third");
            var builder = Module().Parameter(ParameterKey.Third, third).Parameter(ParameterKey.First, first);
            Assert.Throws<InvalidOperationException>(() => builder.Build());
            var contract = builder.Parameter(ParameterKey.Second, second).Build();
            CollectionAssert.AreEqual(new[] { first, second, third }, contract.Parameters);
            var context = Context();
            var contribution = new FluxParticleModuleContribution(context, contract, true);
            contribution.Bind(third, 0.75f);
            contribution.UnavailableParameter(second);
            Assert.Throws<InvalidOperationException>(() => contribution.Complete());
            contribution.Bind(first, 0.25f);
            contribution.Complete();
            var binding = context.ModuleBindings[contract.Id];
            Assert.AreSame(first, binding.Parameters[(int)ParameterKey.First].Contract);
            Assert.IsNull(binding.Parameters[(int)ParameterKey.Second]);
            Assert.AreSame(third, binding.Parameters[(int)ParameterKey.Third].Contract);
        }

        [Test]
        public void EnumKeysRejectDuplicatesUndefinedValuesAndMixedDefinitions()
        {
            var first = Float("test.first", "First");
            var second = Float("test.second", "Second");
            var builder = Module().Parameter(ParameterKey.First, first);
            Assert.Throws<InvalidOperationException>(() => builder.Parameter(ParameterKey.First, second));
            Assert.Throws<InvalidOperationException>(() => builder.Parameter((ParameterKey)99, second));
            Assert.Throws<InvalidOperationException>(() => builder.Parameter(OtherKey.First, second));
            Assert.Throws<InvalidOperationException>(() => builder.Parameter(second));
            Assert.Throws<InvalidOperationException>(() => Module().Parameter(first).Parameter(ParameterKey.Second, second));
            Assert.Throws<ArgumentNullException>(() => builder.Parameter(ParameterKey.Second, null));
            builder.Parameter(ParameterKey.Second, second).Parameter(ParameterKey.Third, Float("test.third", "Third"));
            Assert.AreEqual(3, builder.Build().Parameters.Count);
        }

        [Test]
        public void EnumKeysRejectAliasedSparseNegativeAndOversizedLayouts()
        {
            var parameter = Float("test.value", "Value");
            Assert.Throws<InvalidOperationException>(() => Module().Parameter(AliasedKey.First, parameter));
            Assert.Throws<InvalidOperationException>(() => Module().Parameter(SparseKey.First, parameter));
            Assert.Throws<InvalidOperationException>(() => Module().Parameter(NegativeKey.First, parameter));
            Assert.Throws<InvalidOperationException>(() => Module().Parameter(WideKey.First, parameter));
        }

        [Test]
        public void GravityAndSizeContractsUseTheirRuntimeEnumSlots()
        {
            Assert.AreSame(FluxParticleGravityContract.Acceleration,
                FluxParticleGravityContract.Module.Parameters[(int)TpLab.Flux.FX.Udon.Modules.FluxParticleGravityParameter.Gravity]);
            Assert.AreSame(FluxParticleSizeOverLifetimeContract.EndSize,
                FluxParticleSizeOverLifetimeContract.Module.Parameters[(int)TpLab.Flux.FX.Udon.Modules.FluxParticleSizeOverLifetimeParameter.EndSize]);
        }

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

        [TestCase(FluxParticleColorOverLifetimeMode.Linear)]
        [TestCase(FluxParticleColorOverLifetimeMode.Gradient)]
        public void ColorContractPreservesHdrAndIgnoresUnusedEndColor(FluxParticleColorOverLifetimeMode mode)
        {
            var settings = new FluxParticleColorOverLifetimeModule();
            Set(settings, "mode", mode);
            var hdr = new Color(4, -1, 2, 1.5f);
            Set(settings, "endColor", mode == FluxParticleColorOverLifetimeMode.Linear ? hdr : new Color(float.NaN, 0, 0, 1));
            var context = Context();
            new FluxParticleColorOverLifetimeDefinition().Contribute(settings, context);
            var contract = FluxParticleColorOverLifetimeContract.Module;
            Assert.AreEqual("fluxfx.color-over-lifetime", contract.Id);
            Assert.AreEqual("FluxParticleColorOverLifetimeHandle", contract.HandleName);
            Assert.AreEqual("ColorOverLifetime", contract.AccessorName);
            Assert.AreEqual("EndColor", FluxParticleColorOverLifetimeContract.EndColor.ApiName);
            Assert.AreEqual(1, context.InitialValues["color.enabled"].x);
            var binding = context.ModuleBindings[contract.Id];
            Assert.AreEqual(1, binding.Parameters.Count);
            if (mode == FluxParticleColorOverLifetimeMode.Linear)
            {
                Assert.AreEqual((Vector4)hdr, context.InitialValues["color.end"]);
                Assert.AreSame(FluxParticleColorOverLifetimeContract.EndColor, binding.Parameters[0].Contract);
            }
            else
            {
                Assert.IsNull(binding.Parameters[0]);
                Assert.IsFalse(context.InitialValues.ContainsKey("color.end"));
                Assert.IsTrue(context.Providers.ContainsKey("color.gradient"));
            }
        }

        [TestCase(FluxParticleRotationOverLifetimeMode.Linear)]
        [TestCase(FluxParticleRotationOverLifetimeMode.Curve)]
        public void RotationContractKeepsUnboundedMeshAnglesInBothModes(FluxParticleRotationOverLifetimeMode mode)
        {
            var settings = new FluxParticleRotationOverLifetimeModule();
            Set(settings, "mode", mode);
            var angles = new Vector3(-720, 450, 1080);
            Set(settings, "endRotation", angles);
            var context = new FluxParticleProgramContribution(new FluxParticleDefinitionBuilder(), FluxParticleRenderMode.Mesh);
            new FluxParticleRotationOverLifetimeDefinition().Contribute(settings, context);
            var contract = FluxParticleRotationOverLifetimeContract.Module;
            Assert.AreEqual("tplab.fluxfx.rotation-over-lifetime", contract.Id);
            Assert.AreEqual("FluxParticleRotationOverLifetimeHandle", contract.HandleName);
            Assert.AreEqual("RotationOverLifetime", contract.AccessorName);
            Assert.AreEqual("EndRotation", FluxParticleRotationOverLifetimeContract.EndRotation.ApiName);
            Assert.AreEqual((Vector4)angles, context.InitialValues["rotation.end"]);
            Assert.AreEqual(1, context.InitialValues["rotation.enabled"].x);
            Assert.AreSame(FluxParticleRotationOverLifetimeContract.EndRotation, context.ModuleBindings[contract.Id].Parameters.Single().Contract);
            Assert.AreEqual(mode == FluxParticleRotationOverLifetimeMode.Curve, context.Providers.ContainsKey("rotation.curve"));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void RenderContractsRejectNonFiniteValuesBeforeRegisteringParameters(float value)
        {
            for (var channel = 0; channel < 4; channel++)
            {
                var color = Color.white;
                color[channel] = value;
                var settings = new FluxParticleColorOverLifetimeModule();
                Set(settings, "endColor", color);
                var context = Context();
                var error = Assert.Throws<InvalidOperationException>(() => new FluxParticleColorOverLifetimeDefinition().Contribute(settings, context));
                StringAssert.Contains("color.end", error.Message);
                Assert.IsFalse(context.Parameters.ContainsKey("color.end"));
            }
            foreach (var mode in new[] { FluxParticleRotationOverLifetimeMode.Linear, FluxParticleRotationOverLifetimeMode.Curve })
            {
                for (var axis = 0; axis < 3; axis++)
                {
                    var rotation = Vector3.zero;
                    rotation[axis] = value;
                    var settings = new FluxParticleRotationOverLifetimeModule();
                    Set(settings, "mode", mode);
                    Set(settings, "endRotation", rotation);
                    var context = new FluxParticleProgramContribution(new FluxParticleDefinitionBuilder(), FluxParticleRenderMode.Mesh);
                    var error = Assert.Throws<InvalidOperationException>(() => new FluxParticleRotationOverLifetimeDefinition().Contribute(settings, context));
                    StringAssert.Contains("rotation.end", error.Message);
                    Assert.IsFalse(context.Parameters.ContainsKey("rotation.end"));
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreviewEvaluatesColorAndRotationContractsAndRestarts(bool lookupMode)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var root = new GameObject("Render contract preview test");
            Shader shader = null;
            Material material = null;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var color = new FluxParticleColorOverLifetimeModule();
                Set(color, "endColor", new Color(4, 3, 2, 0.4f));
                var rotation = new FluxParticleRotationOverLifetimeModule();
                Set(rotation, "endRotation", new Vector3(0, 0, 180));
                if (lookupMode)
                {
                    Set(color, "mode", FluxParticleColorOverLifetimeMode.Gradient);
                    var gradient = new Gradient();
                    gradient.SetKeys(new[] { new GradientColorKey(new Color(0.25f, 0.5f, 1), 0), new GradientColorKey(new Color(0.25f, 0.5f, 1), 1) },
                        new[] { new GradientAlphaKey(0.5f, 0), new GradientAlphaKey(0.5f, 1) });
                    Set(color, "gradient", gradient);
                    Set(rotation, "mode", FluxParticleRotationOverLifetimeMode.Curve);
                    Set(rotation, "curve", AnimationCurve.Linear(0, 0, 1, 2));
                }
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("lifetime.lifetime").floatValue = 1;
                serialized.FindProperty("emission.rate").floatValue = 4;
                serialized.FindProperty("render.startColor").colorValue = new Color(2, 0.5f, 0.25f, 0.8f);
                serialized.FindProperty("render.startRotation").vector3Value = new Vector3(0, 0, 10);
                var modules = serialized.FindProperty("modules");
                modules.arraySize = 2;
                modules.GetArrayElementAtIndex(0).managedReferenceValue = color;
                modules.GetArrayElementAtIndex(1).managedReferenceValue = rotation;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var build = FluxParticleBuildPipeline.Build(authoring);
                shader = ShaderUtil.CreateShaderAsset(string.Join("\n", new[]
                {
                    "Shader \"Hidden/FluxFX/Tests/RenderContracts\" { SubShader { Pass { ZTest Always Cull Off ZWrite Off Blend Off",
                    "HLSLPROGRAM", "#pragma target 3.5", "#pragma vertex vert_img", "#pragma fragment frag",
                    "#include \"UnityCG.cginc\"", build.Shaders.RenderSource,
                    "float4 frag(v2f_img i) : SV_Target { bool alive; FluxRenderContext value = FluxFX_EvaluateRender(i.uv, alive);",
                    "return alive ? float4(value.valueRenderColor.r, value.valueRenderColor.a, value.valueRenderRotation.z, 1) : 0; }",
                    "ENDHLSL", "} } Fallback Off }"
                }), true);
                Assert.IsNotNull(shader);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
                material = new Material(shader);
                using (var preview = new FluxParticleEditorPreview(build, authoring, 1))
                {
                    var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                        .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                    var start = lookupMode ? new Color(0.5f, 0.4f, 10, 1) : new Color(2, 0.8f, 10, 1);
                    preview.Step(0.25f);
                    AssertRenderEvaluation(start, session, material);
                    preview.Step(0.5f);
                    AssertRenderEvaluation(lookupMode ? new Color(0.5f, 0.4f, 190, 1) : new Color(3, 0.6f, 100, 1), session, material);
                    preview.Restart();
                    AssertRenderEvaluation(Color.clear, session, material);
                    preview.Step(0.25f);
                    AssertRenderEvaluation(start, session, material);
                }
            }
            finally
            {
                if (material != null) Object.DestroyImmediate(material);
                if (shader != null) Object.DestroyImmediate(shader);
                Object.DestroyImmediate(root);
            }
        }

        static void AssertRenderEvaluation(Color expected, FluxParticleProgramGpuSession session, Material material)
        {
            session.BindRender(material);
            var target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                Graphics.Blit(null, target, material);
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                readback.Apply();
                var actual = readback.GetPixel(0, 0);
                for (var component = 0; component < 4; component++)
                {
                    Assert.AreEqual(expected[component], actual[component], 0.01f, "Component " + component);
                }
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
            }
        }

        static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
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
