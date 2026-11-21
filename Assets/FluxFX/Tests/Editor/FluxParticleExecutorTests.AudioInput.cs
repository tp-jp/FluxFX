using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Authoring;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Compilation.Compiler;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using TpLab.Flux.FX.Editor.Core;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.Udon;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [Test]
        public void AudioInputOutputContractFeedsCurrentStepToConsumer()
        {
            RequireGraphics();
            SetModules(new FluxParticleAudioInputModule());
            var compiled = BuildAudioProbe(true);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 1);
            foreach (var kernel in runtime.GetComponentsInChildren<FluxKernel>(true))
            {
                kernel.SetVector(FluxParticleHlslNames.ProviderProperty("audio-input.sample"), new Vector4(1, 0.5f, 1, 0));
            }
            runtime.Step(0.1f, 0, 0, 1);
            var response = ReadValue(runtime, compiled, FluxParticleAudioInputOutputs.Response, 0);
            Assert.Greater(response.x, 0.3f);
            AssertVector(response, ReadValue(runtime, compiled, "audio-probe.response", 0));
        }

        [Test]
        public void AudioInputAndDistanceFadeCanRunInScenePreview()
        {
            RequireGraphics();
            SetModules(new FluxParticleAudioInputModule(), new FluxParticleDistanceFadeModule());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            using (var preview = new FluxParticleEditorPreview(compiled, authoring, 4))
            {
                preview.Step(0.25f);
                var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                    .GetField("session", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(preview);
                Assert.IsTrue(compiled.Shaders.Compilation.StateLayout.TryGetBinding(FluxParticleAudioInputOutputs.Response, out var binding));
                var value = ReadTexture(session.CurrentPages[binding.PageId], 0);
                for (var component = 0; component < binding.ComponentCount; component++)
                {
                    Assert.AreEqual(0, value[binding.ChannelOffset + component], 0.0001f);
                }
            }
        }

        [Test]
        public void AudioLinkInstalledInputDrivesGpuResponse()
        {
            RequireGraphics();
            if (PackageInfo.FindForAssetPath("Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc") == null)
            {
                Assert.Ignore("Optional AudioLink package is not installed.");
            }
            SetModules(new FluxParticleAudioInputModule());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            Assert.That(compiled.Shaders.Compilation.Providers[FluxParticleAudioInputOutputs.RawBands].HlslPath, Does.EndWith("/AudioLinkInput.hlsl"));
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            var handle = runtime.GetComponentInParent<FluxParticleSystem>().Modules.AudioInput;
            var texture = new Texture2D(128, 64, TextureFormat.RGBAFloat, false, true);
            var empty = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            try
            {
                var bands = new[] { 0.1f, 0.3f, 0.6f, 0.9f };
                var data = new Color[128 * 64];
                for (var i = 0; i < 4; i++) data[i * 128] = new Color(bands[i], 0, 0, 0);
                data[22 * 128 + 1] = new Color(7, 0, 0, 0);
                texture.SetPixels(data);
                texture.Apply();
                empty.SetPixel(0, 0, Color.clear);
                empty.Apply();
                var kernels = runtime.GetComponentsInChildren<FluxKernel>(true);
                Assert.IsTrue(handle.SetAttack(0.01f));
                Assert.IsTrue(handle.SetSlowResponse(0.01f));
                Assert.IsTrue(handle.SetRelease(0.01f));
                foreach (var gain in new[] { 0f, 1f, 2f })
                foreach (var strength in new[] { 0f, 0.5f, 1f })
                for (var band = 0; band < 4; band++)
                {
                    runtime.Clear();
                    Assert.IsTrue(handle.SetBand((FluxParticleAudioBand)band));
                    Assert.IsTrue(handle.SetGain(gain));
                    Assert.IsTrue(handle.SetStrength(strength));
                    Assert.IsTrue(handle.SetNoiseFloor(0.1f));
                    Assert.IsTrue(handle.SetAverageGain(1.5f));
                    foreach (var kernel in kernels) kernel.SetTexture("_AudioTexture", texture);
                    runtime.Step(0, 0, 1, 1);
                    runtime.Step(0.2f, 0, 0, 1);
                    var expectedBand = Mathf.Clamp01((bands[band] - 0.1f) / 0.9f * gain) * strength;
                    var expectedAverage = Mathf.Clamp01((bands.Average() - 0.1f) / 0.9f * gain * 1.5f) * strength;
                    AssertVector(new Vector4(expectedBand, expectedBand, expectedAverage, 0),
                        ReadValue(runtime, compiled, FluxParticleAudioInputOutputs.Response, 0), 0.0001f);
                }
                foreach (var kernel in kernels) kernel.SetTexture("_AudioTexture", empty);
                runtime.Step(0.2f, 0, 0, 1);
                AssertVector(Vector4.zero, ReadValue(runtime, compiled, FluxParticleAudioInputOutputs.Response, 0), 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(empty);
            }
        }

        [Test]
        public void AudioInputWithoutSignalPreservesParticlesAndProducesZero()
        {
            RequireGraphics();
            SetModules(new FluxParticleAudioInputModule());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 1);
            runtime.Step(0.1f, 0, 0, 1);
            Assert.AreEqual(Vector4.zero, ReadValue(runtime, compiled, FluxParticleAudioInputOutputs.Response, 0));
            Assert.AreEqual(0.1f, ReadValue(runtime, compiled, "particle.age", 0).x, 0.0001f);
            Assert.IsFalse(compiled.Shaders.Compilation.Operations.Any(operation =>
                operation.OwnerId == FluxParticleModuleIds.AudioInput && operation.Phase == FluxParticlePhase.Render));
        }

        [TestCase("off")]
        [TestCase("unavailable")]
        [TestCase("silence")]
        [TestCase("stalled")]
        public void AudioInputGpuResponseDecaysAndRecovers(string interruption)
        {
            RequireGraphics();
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("lifetime.lifetime").floatValue = 100;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetModules(new FluxParticleAudioInputModule());
            var compiled = BuildAudioProbe();
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            var handle = runtime.GetComponentInParent<FluxParticleSystem>().Modules.AudioInput;
            Assert.IsNotNull(handle);
            Assert.IsFalse(handle.SetAttack(0));
            Assert.IsFalse(handle.SetBand((FluxParticleAudioBand)4));
            Assert.IsTrue(handle.SetAttack(0.1f));
            Assert.IsTrue(handle.SetRelease(0.1f));
            Assert.IsTrue(handle.SetSlowResponse(0.2f));
            Assert.IsTrue(handle.SetTimeout(0.2f));
            runtime.Step(0, 0, 1, 1);
            var property = FluxParticleHlslNames.ProviderProperty("audio-input.sample");
            var kernels = runtime.GetComponentsInChildren<FluxKernel>(true);
            var expected = Vector3.zero;
            for (var frame = 1; frame <= 28; frame++)
            {
                var interrupted = frame >= 4 && frame <= 23;
                var sample = new Vector3(0.8f, 0.4f, frame);
                if (interrupted && interruption == "unavailable")
                {
                    sample = new Vector3(0, 0, -1);
                }
                if (interrupted && interruption == "silence")
                {
                    sample = new Vector3(0, 0, frame);
                }
                if (interrupted && interruption == "stalled")
                {
                    sample.z = 3;
                }
                Assert.IsTrue(handle.SetEnabled(!interrupted || interruption != "off"));
                if (frame == 24)
                {
                    // Live response-time changes affect already living particles.
                    Assert.IsTrue(handle.SetAttack(0.05f));
                }
                foreach (var kernel in kernels) kernel.SetVector(property, sample);
                runtime.Step(0.1f, 0, 0, 1);
                var muted = interrupted && (interruption != "stalled" || frame >= 5);
                var target = muted ? Vector2.zero : new Vector2(0.8f, 0.4f);
                var attack = frame >= 24 ? 0.05f : 0.1f;
                expected.x = AudioExpected(expected.x, target.x, target.x > expected.x ? attack : 0.1f);
                expected.y = AudioExpected(expected.y, expected.x, 0.2f);
                expected.z = AudioExpected(expected.z, target.y, target.y > expected.z ? attack : 0.1f);
                var actual = ReadValue(runtime, compiled, FluxParticleAudioInputOutputs.Response, 0);
                AssertVector(new Vector4(expected.x, expected.y, expected.z, 0), actual, 0.0002f);
                if (frame == 23)
                {
                    Assert.Less(actual.y, 0.001f, "Slow output must decay while disabled/stopped.");
                }
                if (frame == 28)
                {
                    Assert.Greater(actual.x, 0.79f, "Input must recover without restarting particles.");
                }
            }
        }

        [Test]
        public void InputAndDistanceFadeHandlesRemainLiveAfterFirstStep()
        {
            RequireGraphics();
            SetModules(new FluxParticleAudioInputModule(), new FluxParticleDistanceFadeModule());
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 0);
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            Assert.IsTrue(modules.AudioInput.SetBand(FluxParticleAudioBand.HighMid));
            Assert.IsTrue(modules.AudioInput.SetGain(2));
            Assert.IsTrue(modules.AudioInput.SetNoiseFloor(0.1f));
            Assert.IsTrue(modules.AudioInput.SetStrength(0.5f));
            Assert.IsTrue(modules.AudioInput.SetAverageGain(2));
            Assert.AreEqual(FluxParticleAudioBand.HighMid, modules.AudioInput.GetBand());
            Assert.AreEqual(2, modules.AudioInput.GetGain());
            Assert.AreEqual(0.1f, modules.AudioInput.GetNoiseFloor());
            Assert.AreEqual(0.5f, modules.AudioInput.GetStrength());
            Assert.AreEqual(2, modules.AudioInput.GetAverageGain());
            Assert.IsFalse(modules.AudioInput.SetGain(float.NaN));
            Assert.IsFalse(modules.AudioInput.SetGain(5));
            Assert.IsTrue(modules.DistanceFade.SetStartDistance(2));
            Assert.IsTrue(modules.DistanceFade.SetEndDistance(1));
            Assert.IsTrue(modules.DistanceFade.SetTarget(FluxParticleDistanceFadeTarget.Alpha));
            Assert.AreEqual(2, modules.DistanceFade.GetStartDistance());
            Assert.AreEqual(1, modules.DistanceFade.GetEndDistance());
            Assert.AreEqual(FluxParticleDistanceFadeTarget.Alpha, modules.DistanceFade.GetTarget());
            Assert.AreEqual(2, runtime.RenderMaterial.GetFloat(FluxParticleHlslNames.ParameterProperty("distance-fade.start-distance")));
            Assert.AreEqual(1, runtime.RenderMaterial.GetFloat(FluxParticleHlslNames.ParameterProperty("distance-fade.end-distance")));
            Assert.AreEqual(1, runtime.RenderMaterial.GetFloat(FluxParticleHlslNames.ParameterProperty("distance-fade.target")));
            Assert.IsFalse(modules.DistanceFade.SetEndDistance(-1));
            Assert.IsFalse(modules.DistanceFade.SetTarget((FluxParticleDistanceFadeTarget)2));
        }

        FluxParticleBuildResult BuildAudioProbe(bool consumer = false, bool rawSources = false, bool noAudioLink = false)
        {
            // Replace only the external sample source; all shipping state/update operations remain intact.
            var original = FluxParticleAuthoringDefinitionBuilder.Create(authoring, out var initial, out var bindings);
            var definitions = new FluxParticleDefinitionBuilder();
            foreach (var value in original.Values.Values) definitions.AddValue(value);
            foreach (var parameter in original.Parameters.Values) definitions.AddParameter(parameter);
            foreach (var provider in original.Providers.Values)
            {
                if (noAudioLink)
                {
                    definitions.AddProvider(provider.HlslPath != null && provider.HlslPath.EndsWith("/AudioLinkInput.hlsl")
                        ? new FluxParticleValueProvider(provider.ValueId, provider.Kind, provider.Inputs, provider.Parameters,
                            provider.HlslPath.Replace("AudioLinkInput.hlsl", "NoAudioInput.hlsl"), provider.EntryPoint) : provider);
                    continue;
                }
                definitions.AddProvider((rawSources ? provider.ValueId == FluxParticleAudioInputOutputs.RawBands || provider.ValueId == FluxParticleAudioInputOutputs.Frame : provider.ValueId == "audio-input.sample")
                    ? new FluxParticleValueProvider(provider.ValueId, FluxParticleProviderKind.Uniform) : provider);
            }
            foreach (var id in original.OperationOrder) definitions.AddOperation(original.Operations[id]);
            foreach (var dependency in original.OptionalDependencies) definitions.AddOptionalDependency(dependency);
            if (consumer)
            {
                const string implementation = "Assets/FluxFX/Tests/Editor/Fixtures/AudioInputConsumer.hlsl";
                definitions.AddValue(new FluxParticleValueDefinition("audio-probe.response", FluxParticleValueType.Float3,
                    FluxParticleWritePolicy.OwnerManaged, "audio-probe"));
                definitions.AddOperation(new FluxParticleOperationDefinition("audio-probe.initialize", "audio-probe", FluxParticlePhase.Spawn,
                    new FluxParticleValueRead[0], new[] { new FluxParticleValueWrite("audio-probe.response", FluxParticleWriteKind.Initialize) },
                    new string[0], implementation, "AudioProbe_Initialize"));
                definitions.AddOperation(new FluxParticleOperationDefinition("audio-probe.update", "audio-probe", FluxParticlePhase.Update,
                    new[] { FluxParticleAudioInputOutputs.Read(FluxParticlePhase.Update) },
                    new[] { new FluxParticleValueWrite("audio-probe.response", FluxParticleWriteKind.Replace) },
                    new[] { FluxParticleAudioInputOutputs.UpdateOperation }, implementation, "AudioProbe_Update"));
            }
            FluxParticleVelocityPipelineResolver.Resolve(definitions);
            FluxParticleValuePipelineResolver.Resolve(definitions, "render.color", definitions.OptionalDependencies, FluxParticlePhase.Render);
            var compilation = new FluxParticleProgramCompiler().Compile(definitions);
            var shaders = new FluxParticleProgramShaderGenerator().Generate(compilation, "Hidden/FluxFX/AudioProbe", FluxParticleCoreDefinitions.CreateLifecycle());
            return new FluxParticleBuildResult(shaders, initial,
                FluxParticleRenderShaderGenerator.Generate(shaders, authoring.Render.Mode), bindings);
        }

        static float AudioExpected(float value, float target, float seconds)
        {
            var next = target + (value - target) * Mathf.Exp(-0.1f / seconds);
            return target == 0 && next < 0.00001f ? 0 : next;
        }
    }
}
