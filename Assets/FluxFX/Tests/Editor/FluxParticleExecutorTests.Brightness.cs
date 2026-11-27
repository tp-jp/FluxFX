using System;
using System.IO;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation.ShaderGeneration;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.Udon;
using UnityEngine;
using UnityEditor;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [Test]
        public void BrightnessAudioRespawnInitializesState()
        {
            RequireGraphics();
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true}", brightness);
            SetModules(new FluxParticleAudioInputModule(), brightness);
            using (var settings = new SerializedObject(authoring))
            {
                settings.FindProperty("lifetime.lifetime").floatValue = 0.2f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var build = BuildAudioProbe();
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 1);
            ObserveAudioBaseline(runtime);
            foreach (var kernel in runtime.GetComponentsInChildren<FluxKernel>(true))
            {
                kernel.SetVector(FluxParticleHlslNames.ProviderProperty("audio-input.sample"), new Vector4(1, 1, 1, 0));
            }
            runtime.Step(0.1f, 0, 0, 1);
            Assert.Greater(ReadValue(runtime, build, "brightness.audio", 0).x, 0.9f);
            runtime.Step(0.2f, 0, 0, 1);
            runtime.Step(0, 0, 1, 1234);
            Assert.AreEqual(1234, ReadValue(runtime, build, "particle.spawn-key", 0).x);
            Assert.AreEqual(0, ReadValue(runtime, build, "particle.age", 0).x);
            Assert.AreEqual(0, ReadValue(runtime, build, "brightness.audio", 0).x);
        }

        [TestCase("silence")]
        [TestCase("stalled")]
        [TestCase("missing")]
        [TestCase("input-off")]
        [TestCase("brightness-off")]
        public void BrightnessAudioUsesRawInputAndIndependentEnvelope(string interruption)
        {
            RequireGraphics();
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true,\"audioAttack\":0.025,\"audioRelease\":0.12}", brightness);
            SetModules(new FluxParticleAudioInputModule(), brightness);
            using (var settings = new SerializedObject(authoring))
            {
                settings.FindProperty("lifetime").FindPropertyRelative("lifetime").floatValue = 60;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var build = BuildAudioProbe();
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 1, 1);
            var modules = runtime.GetComponentInParent<FluxParticleSystem>().Modules;
            var handle = modules.Brightness;
            Assert.IsNotNull(handle);
            Assert.IsTrue(handle.SetPeriod(5));
            Assert.IsFalse(handle.SetPeriod(-1));
            Assert.AreEqual(5, handle.GetPeriod());
            var expected = 0f;
            var maximumError = 0f;
            var stale = 0f;
            var previousFrame = -1f;
            for (var frame = 0; frame < 60; frame++)
            {
                var interrupted = frame >= 10 && frame < 40;
                var sampleFrame = interrupted && interruption == "stalled" ? 9 : frame;
                if (interrupted && interruption == "missing")
                {
                    sampleFrame = -1;
                }
                var inputEnabled = !interrupted || interruption != "input-off";
                var brightnessEnabled = !interrupted || interruption != "brightness-off";
                Assert.IsTrue(modules.AudioInput.SetEnabled(inputEnabled));
                Assert.IsTrue(handle.SetEnabled(brightnessEnabled));
                var level = interrupted && interruption == "silence" ? 0f : 0.8f;
                foreach (var kernel in runtime.GetComponentsInChildren<FluxKernel>(true))
                {
                    kernel.SetVector(FluxParticleHlslNames.ProviderProperty("audio-input.sample"), new Vector4(0.2f, level, sampleFrame, 0));
                }
                stale = sampleFrame >= 0 && sampleFrame != previousFrame ? 0 : Mathf.Min(stale + 0.1f, 1);
                previousFrame = sampleFrame;
                var awaitingUpdate = frame == 0 || frame == 40 && (interruption == "missing" || interruption == "input-off");
                var target = !awaitingUpdate && inputEnabled && brightnessEnabled && sampleFrame >= 0 && stale < 1 ? level : 0;
                expected = AudioExpected(expected, target, target > expected ? 0.025f : 0.12f);
                runtime.Step(0.1f, 0, 0, 1);
                var actual = ReadValue(runtime, build, "brightness.audio", 0).x;
                maximumError = Mathf.Max(maximumError, Mathf.Abs(expected - actual));
                Assert.AreEqual(expected, actual, 0.0001f, "frame " + frame);
                if (frame == 39)
                {
                    Assert.Less(actual, 0.00001f);
                }
                if (frame == 59)
                {
                    Assert.Greater(actual, 0.79f);
                }
            }
            Directory.CreateDirectory("Temp/BrightnessIntegration/Gpu");
            File.WriteAllText("Temp/BrightnessIntegration/Gpu/audio-" + interruption + ".txt", "60 GPU updates; max absolute error " + maximumError.ToString("R"));
        }
    }
}
