using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Authoring;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Editor.Modules.Brightness;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [Test]
        public void AudioChannelsPreserveLegacyDefaultsAndStableReferences()
        {
            var input = new FluxParticleAudioInputModule();
            JsonUtility.FromJsonOverwrite("{\"band\":2,\"gain\":1.7,\"noiseFloor\":0.1,\"strength\":0.8,\"attack\":0.3,\"release\":0.9,\"slowResponse\":0.6,\"averageGain\":1.2,\"timeout\":2}", input);
            Assert.IsEmpty(input.Channels);
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true,\"baseMinimum\":6,\"baseMaximum\":10,\"audioAttack\":0.025,\"audioRelease\":0.12}", brightness);
            Assert.AreEqual(FluxParticleAudioSource.Average, brightness.AudioSource);
            Assert.IsTrue(string.IsNullOrEmpty(brightness.AudioChannel));
            SetModules(input, brightness);
            var before = FluxParticleAuthoringDefinitionBuilder.Create(authoring, out var original, out _);
            SetChannels(input, Channel("first", FluxParticleAudioSource.Bass, 0.1f), Channel("second", FluxParticleAudioSource.Bass, 1));
            JsonUtility.FromJsonOverwrite("{\"audioChannel\":\"first\"}", brightness);
            var first = FluxParticleBuildPipeline.Build(authoring);
            var channel = input.Channels[0];
            JsonUtility.FromJsonOverwrite("{\"name\":\"Renamed\"}", channel);
            SetChannels(input, input.Channels[1], channel);
            var second = FluxParticleBuildPipeline.Build(authoring);
            Assert.AreEqual("first", brightness.AudioChannel);
            Assert.AreEqual("Renamed", input.Channels[1].Name);
            var ids = first.Shaders.Compilation.Parameters.Keys.OrderBy(id => id);
            CollectionAssert.AreEqual(ids, second.Shaders.Compilation.Parameters.Keys.OrderBy(id => id));
            foreach (var pair in original.Where(pair => pair.Key.StartsWith("audio-input.")))
                Assert.AreEqual(pair.Value, second.InitialValues[pair.Key], pair.Key);
            Assert.AreEqual(6, brightness.BaseMinimum);
            Assert.AreEqual(10, brightness.BaseMaximum);
            Assert.IsFalse(second.Shaders.Compilation.StateLayout.TryGetBinding("brightness.audio", out _), "Channels must not add a second envelope.");
            var renderInput = second.Shaders.Compilation.Providers["brightness.audio"].Inputs.Single();
            Assert.AreEqual(FluxParticleAudioInputOutputs.ChannelResponse("first"), renderInput);
            Assert.IsFalse(before.Values.ContainsKey(FluxParticleAudioInputOutputs.ChannelResponse("first")));
        }

        [TestCase("duplicate")]
        [TestCase("empty")]
        [TestCase("invalid")]
        [TestCase("missing")]
        public void AudioChannelsRejectBrokenReferencesWithoutRewritingData(string issue)
        {
            var input = new FluxParticleAudioInputModule();
            var first = Channel("first", FluxParticleAudioSource.Bass, 0.1f);
            var second = Channel(issue == "duplicate" ? "first" : "second", FluxParticleAudioSource.Treble, 0.2f);
            if (issue == "empty" || issue == "invalid")
                JsonUtility.FromJsonOverwrite(issue == "empty" ? "{\"id\":\"\"}" : "{\"id\":\"Bad ID\"}", first);
            SetChannels(input, first, second);
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true,\"audioChannel\":\"missing\"}", brightness);
            SetModules(input, brightness);
            var before = JsonUtility.ToJson(input);
            Assert.That(() => FluxParticleBuildPipeline.Build(authoring), Throws.Exception);
            Assert.AreEqual(before, JsonUtility.ToJson(input));
            Assert.AreEqual("missing", brightness.AudioChannel);
        }

        [Test]
        public void AudioChannelsGpuExposeAllBandsAndPreserveAverageBeforeShaping()
        {
            RequireGraphics();
            var input = new FluxParticleAudioInputModule();
            JsonUtility.FromJsonOverwrite("{\"gain\":2,\"noiseFloor\":0.2,\"strength\":0.7,\"averageGain\":1.5}", input);
            var sources = new[] { FluxParticleAudioSource.Average, FluxParticleAudioSource.Bass, FluxParticleAudioSource.LowMid,
                FluxParticleAudioSource.HighMid, FluxParticleAudioSource.Treble };
            SetChannels(input, sources.Select((source, i) => Channel("channel" + i, source, 0.2f)).ToArray());
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true}", brightness);
            SetModules(input, brightness);
            var build = BuildAudioProbe(rawSources: true);
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 1);
            var bands = new Vector4(0.05f, 0.3f, 0.6f, 1.2f);
            ObserveAudioBaseline(runtime, true);
            var maximumError = 0f;
            var expectedBrightness = 0f;
            var state = new float[5];
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            for (var frame = 1; frame <= 5; frame++)
            {
                SetRawAudio(runtime, bands, frame);
                Assert.IsTrue(modules.Brightness.SetAudioSource(sources[frame - 1]));
                runtime.Step(0.1f, 0, 0, 1);
                var average = 0f;
                for (var j = 0; j < 4; j++) average += bands[j] * 0.25f;
                for (var i = 0; i < 5; i++)
                {
                    var raw = i == 0 ? average : bands[i - 1];
                    var target = Mathf.Clamp01((raw - 0.2f) / 0.8f * 2 * (i == 0 ? 1.5f : 1)) * 0.7f;
                    state[i] = AudioExpected(state[i], target, 0.2f);
                    var actual = ReadValue(runtime, build, FluxParticleAudioInputOutputs.ChannelResponse("channel" + i), 0).x;
                    maximumError = Mathf.Max(maximumError, Mathf.Abs(state[i] - actual));
                    Assert.AreEqual(state[i], actual, 0.00002f, sources[i].ToString());
                    if (i == frame - 1)
                    {
                        expectedBrightness = AudioExpected(expectedBrightness, target, target > expectedBrightness ? 0.025f : 0.12f);
                    }
                }
                Assert.AreEqual(expectedBrightness, ReadValue(runtime, build, "brightness.audio", 0).x, 0.00002f);
            }
            Assert.IsFalse(modules.Brightness.SetAudioSource((FluxParticleAudioSource)5));
            Assert.AreEqual(FluxParticleAudioSource.Treble, modules.Brightness.GetAudioSource());
            Directory.CreateDirectory("Temp/AudioChannels");
            File.WriteAllText("Temp/AudioChannels/GpuError.txt", "Four bands and average maximum absolute error: " + maximumError.ToString("R"));
        }

        [TestCase("silence")]
        [TestCase("unavailable")]
        [TestCase("off")]
        [TestCase("stalled")]
        public void AudioChannelsGpuIndependentResponsesDecayAndRecover(string interruption)
        {
            RequireGraphics();
            var input = new FluxParticleAudioInputModule();
            JsonUtility.FromJsonOverwrite("{\"noiseFloor\":0,\"timeout\":0.2}", input);
            SetChannels(input, Channel("fast", FluxParticleAudioSource.Bass, 0.05f), Channel("slow", FluxParticleAudioSource.Bass, 0.5f));
            SetModules(input);
            using (var settings = new SerializedObject(authoring))
            {
                settings.FindProperty("lifetime.lifetime").floatValue = 100;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var build = BuildAudioProbe(rawSources: true);
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 1);
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            var fast = modules.GetAudioChannel("fast");
            var slow = modules.GetAudioChannel("slow");
            Assert.IsNotNull(fast);
            Assert.AreSame(fast, modules.GetAudioChannel("fast"));
            Assert.IsNull(modules.GetAudioChannel("deleted"));
            Assert.IsFalse(fast.SetAttack(0));
            Assert.IsFalse(fast.SetGain(float.NaN));
            Assert.IsFalse(fast.SetSource((FluxParticleAudioSource)5));
            var fastValue = 0f;
            var slowValue = 0f;
            ObserveAudioBaseline(runtime, true);
            for (var frame = 1; frame <= 28; frame++)
            {
                var interrupted = frame >= 4 && frame <= 23;
                var counter = interrupted && interruption == "unavailable" ? -1 : interrupted && interruption == "stalled" ? 3 : frame;
                SetRawAudio(runtime, interrupted && interruption == "silence" ? Vector4.zero : new Vector4(0.8f, 0.1f, 0.4f, 0.6f), counter);
                modules.AudioInput.SetEnabled(!interrupted || interruption != "off");
                if (frame == 24)
                {
                    Assert.IsTrue(fast.SetSource(FluxParticleAudioSource.Treble));
                    Assert.IsTrue(fast.SetGain(0.5f));
                    Assert.IsTrue(fast.SetAttack(0.02f));
                    Assert.IsTrue(fast.SetRelease(0.15f));
                }
                runtime.Step(0.1f, 0, 0, 1);
                var muted = interrupted && (interruption != "stalled" || frame >= 5)
                    || frame == 24 && (interruption == "off" || interruption == "unavailable");
                var target = muted ? 0 : 0.8f;
                var fastTarget = muted ? 0 : frame >= 24 ? 0.3f : target;
                fastValue = AudioExpected(fastValue, fastTarget, fastTarget > fastValue ? (frame >= 24 ? 0.02f : 0.05f) : (frame >= 24 ? 0.15f : 0.2f));
                slowValue = AudioExpected(slowValue, target, target > slowValue ? 0.5f : 0.2f);
                Assert.AreEqual(fastValue, ReadValue(runtime, build, FluxParticleAudioInputOutputs.ChannelResponse("fast"), 0).x, 0.0001f, "fast frame " + frame);
                Assert.AreEqual(slowValue, ReadValue(runtime, build, FluxParticleAudioInputOutputs.ChannelResponse("slow"), 0).x, 0.0001f, "slow frame " + frame);
            }
            Assert.AreEqual(FluxParticleAudioSource.Treble, fast.GetSource());
            Assert.AreEqual(0.5f, fast.GetGain());
            Assert.AreEqual(0.02f, fast.GetAttack());
            Assert.AreEqual(0.15f, fast.GetRelease());
        }

        [Test]
        public void AudioChannelsInspectorAddsUniqueIdsAndRegistersLiveFields()
        {
            var input = new FluxParticleAudioInputModule();
            SetModules(input);
            using (var serialized = new SerializedObject(authoring))
            {
                var channels = serialized.FindProperty("modules").GetArrayElementAtIndex(0).FindPropertyRelative("channels");
                var add = typeof(TpLab.Flux.FX.Editor.Inspector.FluxParticleAuthoringEditor)
                    .GetMethod("AddAudioChannel", BindingFlags.Static | BindingFlags.NonPublic);
                add.Invoke(null, new object[] { channels });
                add.Invoke(null, new object[] { channels });
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            input = (FluxParticleAudioInputModule)authoring.Modules[0];
            Assert.AreEqual(2, input.Channels.Count);
            Assert.AreNotEqual(input.Channels[0].Id, input.Channels[1].Id);
            Assert.AreEqual(32, input.Channels[0].Id.Length);
            var editor = UnityEditor.Editor.CreateEditor(authoring);
            try
            {
                editor.GetType().GetMethod("InitializeLiveInspector", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(editor, null);
                var fields = (System.Collections.IDictionary)editor.GetType().GetField("_liveFields", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor);
                for (var i = 0; i < 2; i++)
                {
                    foreach (var name in new[] { "source", "gain", "attack", "release" })
                    {
                        var path = "modules.Array.data[0].channels.Array.data[" + i + "]." + name;
                        Assert.IsTrue(fields.Contains(path), path);
                        var binding = (TpLab.Flux.FX.Editor.Inspector.FluxParticleLiveBinding)fields[path].GetType().GetField("Binding").GetValue(fields[path]);
                        Assert.AreEqual("audio-input.channel." + input.Channels[i].Id + "." + name, binding.Contract.Id);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(editor); }
        }

        [Test]
        public void AudioChannelsPrefabRoundTripPreservesIdsAndBrightnessReference()
        {
            var input = new FluxParticleAudioInputModule();
            SetChannels(input, Channel("first", FluxParticleAudioSource.LowMid, 0.1f), Channel("second", FluxParticleAudioSource.Treble, 0.3f));
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true,\"audioChannel\":\"first\"}", brightness);
            SetModules(input, brightness);
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/FluxFXTestTemp", Path.GetFileName(folder));
            var path = folder + "/Channels.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            var loaded = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var settings = loaded.GetComponent<TpLab.Flux.FX.Scripts.FluxParticleAuthoring>();
                var savedInput = (FluxParticleAudioInputModule)settings.Modules[0];
                var savedBrightness = (FluxParticleBrightnessModule)settings.Modules[1];
                Assert.AreEqual("first", savedInput.Channels[0].Id);
                Assert.AreEqual("second", savedInput.Channels[1].Id);
                Assert.AreEqual("first", savedBrightness.AudioChannel);
                Assert.AreEqual(0.1f, savedInput.Channels[0].Attack);
                var reordered = new[] { savedInput.Channels[1], savedInput.Channels[0] };
                SetChannels(savedInput, reordered);
                JsonUtility.FromJsonOverwrite("{\"name\":\"Renamed\"}", savedInput.Channels[1]);
                PrefabUtility.SaveAsPrefabAsset(loaded, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(loaded); }
            loaded = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var settings = loaded.GetComponent<TpLab.Flux.FX.Scripts.FluxParticleAuthoring>();
                var savedInput = (FluxParticleAudioInputModule)settings.Modules[0];
                Assert.AreEqual("first", savedInput.Channels[1].Id);
                Assert.AreEqual("Renamed", savedInput.Channels[1].Name);
                Assert.AreEqual("first", ((FluxParticleBrightnessModule)settings.Modules[1]).AudioChannel);
                var build = FluxParticleBuildPipeline.Build(settings);
                Assert.AreEqual(FluxParticleAudioInputOutputs.ChannelResponse("first"), build.Shaders.Compilation.Providers["brightness.audio"].Inputs.Single());
            }
            finally { PrefabUtility.UnloadPrefabContents(loaded); }
        }

        [Test]
        public void AudioChannelsOptionalPackageFallbackProducesZeroWithoutAudioLinkIncludes()
        {
            RequireGraphics();
            var input = new FluxParticleAudioInputModule();
            SetChannels(input, Channel("fallback", FluxParticleAudioSource.Treble, 0.1f));
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true}", brightness);
            SetModules(input, brightness);
            var build = BuildAudioProbe(noAudioLink: true);
            Assert.That(build.Shaders.Compilation.Providers[FluxParticleAudioInputOutputs.RawBands].HlslPath, Does.EndWith("NoAudioInput.hlsl"));
            Assert.That(build.Shaders.Compilation.Providers[FluxParticleAudioInputOutputs.Frame].HlslPath, Does.EndWith("NoAudioInput.hlsl"));
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 1);
            runtime.Step(0.1f, 0, 0, 1);
            Assert.AreEqual(0, ReadValue(runtime, build, FluxParticleAudioInputOutputs.ChannelResponse("fallback"), 0).x);
            Assert.AreEqual(0, ReadValue(runtime, build, "brightness.audio", 0).x);
            Assert.AreEqual(0.1f, ReadValue(runtime, build, "particle.age", 0).x, 0.00001f);
        }

        static FluxParticleAudioChannel Channel(string id, FluxParticleAudioSource source, float attack)
        {
            var channel = new FluxParticleAudioChannel();
            JsonUtility.FromJsonOverwrite("{\"id\":\"" + id + "\",\"source\":" + (int)source + ",\"attack\":" + attack.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"release\":0.2}", channel);
            return channel;
        }

        static void SetChannels(FluxParticleAudioInputModule input, params FluxParticleAudioChannel[] channels)
        {
            typeof(FluxParticleAudioInputModule).GetField("channels", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, channels);
        }

        static void SetRawAudio(FluxParticleExecutor runtime, Vector4 bands, float counter)
        {
            foreach (var kernel in runtime.GetComponentsInChildren<FluxKernel>(true))
            {
                kernel.SetVector(FluxParticleHlslNames.ProviderProperty(FluxParticleAudioInputOutputs.RawBands), bands);
                kernel.SetFloat(FluxParticleHlslNames.ProviderProperty(FluxParticleAudioInputOutputs.Frame), counter);
            }
        }
    }
}
