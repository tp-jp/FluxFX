using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.Udon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void AudioInputFreshnessRequiresUpdatesAcrossStartupDisconnectOffAndWrap(bool audioLinkTexture)
        {
            RequireGraphics();
            if (audioLinkTexture && UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc") == null)
            {
                Assert.Ignore("Optional AudioLink package is not installed.");
            }
            ConfigureFreshnessProbe();
            var build = audioLinkTexture ? FluxParticleBuildPipeline.Build(authoring) : BuildAudioProbe(rawSources: true);
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            var handle = runtime.GetComponentInParent<FluxParticleSystem>().Modules.AudioInput;
            var bands = new Vector4(0, 0.4318854f, 0.140429f, 0);
            var texture = new Texture2D(128, 64, TextureFormat.RGBAFloat, false, true);
            var missing = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var kernels = runtime.GetComponentsInChildren<FluxKernel>(true);
            var expected = new float[5];
            var maximumError = 0f;
            var report = new List<string> { "label,counter,active,response,slow,average,channel,brightness" };
            try
            {
                runtime.Step(0, 0, 1, 1);
                // 比較用の有効／無効はシナリオから指定し、実装の鮮度判定を複製しない。
                void Check(string label, float counter, bool active, bool enabled = true, bool silent = false)
                {
                    var signal = silent ? Vector4.zero : bands;
                    Assert.IsTrue(handle.SetEnabled(enabled));
                    if (audioLinkTexture)
                    {
                        for (var i = 0; i < 4; i++) texture.SetPixel(0, i, new Color(signal[i], 0, 0, 0));
                        texture.SetPixel(1, 22, new Color(counter, 0, 0, 0));
                        texture.Apply();
                        foreach (var kernel in kernels) kernel.SetTexture("_AudioTexture", counter < 0 ? missing : texture);
                    }
                    else
                    {
                        SetRawAudio(runtime, signal, counter < 0 ? -1 : counter % 1024);
                    }
                    runtime.Step(0.1f, 0, 0, 1);
                    var selected = active ? signal.y : 0;
                    var average = active ? (signal.x + signal.y + signal.z + signal.w) * 0.25f : 0;
                    expected[0] = AudioExpected(expected[0], selected, selected > expected[0] ? 0.05f : 0.2f);
                    expected[1] = AudioExpected(expected[1], expected[0], 0.3f);
                    expected[2] = AudioExpected(expected[2], average, average > expected[2] ? 0.05f : 0.2f);
                    expected[3] = AudioExpected(expected[3], selected, selected > expected[3] ? 0.1f : 0.2f);
                    expected[4] = AudioExpected(expected[4], average, average > expected[4] ? 0.025f : 0.12f);
                    var response = ReadValue(runtime, build, FluxParticleAudioInputOutputs.Response, 0);
                    var actual = new[] { response.x, response.y, response.z,
                        ReadValue(runtime, build, FluxParticleAudioInputOutputs.ChannelResponse("freshness"), 0).x,
                        ReadValue(runtime, build, "brightness.audio", 0).x };
                    for (var i = 0; i < actual.Length; i++)
                    {
                        maximumError = Mathf.Max(maximumError, Mathf.Abs(actual[i] - expected[i]));
                        Assert.AreEqual(expected[i], actual[i], 0.00002f, label + " output " + i);
                    }
                    var freshness = ReadValue(runtime, build, FluxParticleAudioInputOutputs.Freshness, 0);
                    Assert.AreEqual(active, enabled && freshness.x >= 0 && freshness.y < 0.25f, label + " validity");
                    report.Add(label + "," + counter + "," + active + "," + string.Join(",", actual.Select(value => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
                }

                for (var i = 0; i < 60; i++) Check("frozen-start-" + i, 7047, false);
                Assert.IsTrue(expected.All(value => value == 0), "Frozen startup must never raise any envelope.");
                Check("silent-first-change", 7048, true, silent: true);
                Check("silent-next-change", 7049, true, silent: true);
                Assert.IsTrue(expected.All(value => value == 0), "Updating silent input must not raise an envelope.");
                Check("missing", -1, false);
                Check("arrival-baseline", 10, false);
                Check("arrival-frozen", 10, false);
                Check("first-change", 11, true);
                Check("normal", 12, true);
                Check("before-wrap", 1023, true);
                Check("wrap", 1024, true);
                Check("silence-updates", 1025, true, silent: true);
                Check("normal-again", 1026, true);
                Check("stopped-0.1", 1026, true);
                Check("stopped-0.2", 1026, true);
                for (var i = 0; i < 25; i++) Check("timed-out-" + i, 1026, false);
                Check("resume", 1027, true);
                Check("off", 1028, false, enabled: false);
                Check("reenable-baseline", 1029, false);
                Check("reenable-frozen", 1029, false);
                Check("reenable-change", 1030, true);
                Check("disconnect-active", -1, false);
                Check("reconnect-other-counter", 999, false);
                Check("reconnect-frozen", 999, false);
                Check("reconnect-change", 1000, true);
                runtime.Clear();
                Array.Clear(expected, 0, expected.Length);
                runtime.Step(0, 0, 1, 2);
                for (var i = 0; i < 10; i++) Check("clear-restart-" + i, 1000, false);
                Check("clear-restart-change", 1001, true);
                Directory.CreateDirectory("Temp/AudioInputFreshness");
                File.WriteAllLines("Temp/AudioInputFreshness/gpu-" + audioLinkTexture + ".csv", report);
                File.WriteAllText("Temp/AudioInputFreshness/error-" + audioLinkTexture + ".txt", maximumError.ToString("R"));
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(missing);
            }
        }

        [Test]
        public void AudioInputFreshnessPreviewRestartDoesNotAcceptResidualBands()
        {
            RequireGraphics();
            ConfigureFreshnessProbe();
            var build = BuildAudioProbe(rawSources: true);
            using (var preview = new FluxParticleEditorPreview(build, authoring, 4))
            {
                var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                    .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                var ids = new[] { FluxParticleAudioInputOutputs.Response,
                    FluxParticleAudioInputOutputs.ChannelResponse("freshness"), "brightness.audio" };
                var peak = 0f;
                for (var restart = 0; restart < 2; restart++)
                {
                    preview.Restart();
                    session.SetUniformVector(FluxParticleAudioInputOutputs.RawBands, new Vector4(0, 0.4318854f, 0.140429f, 0));
                    session.SetUniformFloat(FluxParticleAudioInputOutputs.Frame, 903);
                    for (var step = 0; step < 60; step++)
                    {
                        preview.Step(0.1f);
                        foreach (var id in ids)
                        {
                            Assert.IsTrue(build.Shaders.Compilation.StateLayout.TryGetBinding(id, out var binding));
                            var value = ReadTexture(session.CurrentPages[binding.PageId], 0);
                            for (var component = 0; component < binding.ComponentCount; component++)
                            {
                                peak = Mathf.Max(peak, Mathf.Abs(value[binding.ChannelOffset + component]));
                            }
                        }
                        Assert.AreEqual(0, peak, "Preview restart " + restart + " step " + step);
                    }
                    session.SetUniformFloat(FluxParticleAudioInputOutputs.Frame, 904);
                    preview.Step(0.1f);
                    foreach (var id in ids)
                    {
                        Assert.IsTrue(build.Shaders.Compilation.StateLayout.TryGetBinding(id, out var binding));
                        Assert.Greater(ReadTexture(session.CurrentPages[binding.PageId], 0)[binding.ChannelOffset], 0, id);
                    }
                }
                Directory.CreateDirectory("Temp/AudioInputFreshness");
                File.WriteAllText("Temp/AudioInputFreshness/preview.txt", "Two starts, 60 frozen steps each; maximum residual response: " + peak);
            }
        }

        void ConfigureFreshnessProbe()
        {
            var input = new FluxParticleAudioInputModule();
            JsonUtility.FromJsonOverwrite("{\"band\":1,\"noiseFloor\":0,\"averageGain\":1,\"attack\":0.05,\"release\":0.2,\"slowResponse\":0.3,\"timeout\":0.25}", input);
            SetChannels(input, Channel("freshness", FluxParticleAudioSource.LowMid, 0.1f));
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true}", brightness);
            SetModules(input, brightness);
            using (var settings = new SerializedObject(authoring))
            {
                settings.FindProperty("lifetime.lifetime").floatValue = 100;
                settings.FindProperty("emission.rate").floatValue = 10;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
