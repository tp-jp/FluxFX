using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(1)]
        [TestCase(4)]
        public void AudioChannelsPreviewPreservesAverageBrightnessImages(int samples)
        {
            RequireGraphics();
            var input = new FluxParticleAudioInputModule();
            var brightness = new FluxParticleBrightnessModule();
            JsonUtility.FromJsonOverwrite("{\"baseMinimum\":6,\"baseMaximum\":10,\"waveform\":1,\"period\":8,\"periodMinimum\":0.8,\"periodMaximum\":1.2,\"randomPhase\":1,\"minimum\":0.75,\"maximum\":1.25,\"audioEnabled\":true}", brightness);
            SetModules(input, brightness);
            ConfigureAudioPreview();
            var reference = CaptureAudioPreview(BuildAudioProbe(), samples, true, "reference");
            var actual = CaptureAudioPreview(BuildAudioProbe(rawSources: true), samples, false, "current");
            var difference = 0f;
            var peak = 0f;
            for (var i = 0; i < reference.Length; i++)
            {
                for (var c = 0; c < 4; c++) difference = Mathf.Max(difference, Mathf.Abs(reference[i][c] - actual[i][c]));
                peak = Mathf.Max(peak, actual[i].maxColorComponent);
            }
            Assert.Greater(peak, 1, "HDR particles must be visible.");
            Assert.LessOrEqual(difference, 0.0001f);
            File.WriteAllText("Temp/AudioChannels/Preview-msaa" + samples + ".txt", "Maximum absolute image difference: " + difference.ToString("R") + "\nPeak: " + peak.ToString("R"));
        }

        [Test]
        public void AudioChannelsPreviewUsesChannelEnvelopeWithoutSecondSmoothing()
        {
            RequireGraphics();
            var input = new FluxParticleAudioInputModule();
            var brightness = new FluxParticleBrightnessModule();
            var channel = Channel("preview", FluxParticleAudioSource.Average, 0.025f);
            JsonUtility.FromJsonOverwrite("{\"release\":0.12}", channel);
            SetChannels(input, channel);
            JsonUtility.FromJsonOverwrite("{\"audioEnabled\":true,\"baseMinimum\":6,\"baseMaximum\":10}", brightness);
            SetModules(input, brightness);
            ConfigureAudioPreview();
            var independent = CaptureAudioPreview(BuildAudioProbe(rawSources: true), 1, false, "independent");
            JsonUtility.FromJsonOverwrite("{\"audioChannel\":\"preview\",\"audioAttack\":10,\"audioRelease\":10}", brightness);
            var shared = CaptureAudioPreview(BuildAudioProbe(rawSources: true), 1, false, "channel");
            var difference = 0f;
            var peak = 0f;
            for (var i = 0; i < shared.Length; i++)
            {
                for (var c = 0; c < 4; c++) difference = Mathf.Max(difference, Mathf.Abs(shared[i][c] - independent[i][c]));
                peak = Mathf.Max(peak, shared[i].maxColorComponent);
            }
            Assert.Greater(peak, 1);
            Assert.LessOrEqual(difference, 0.0001f);
            File.WriteAllText("Temp/AudioChannels/Preview-channel.txt", "Channel versus independent envelope maximum image difference: " + difference.ToString("R"));
        }

        void ConfigureAudioPreview()
        {
            using (var settings = new SerializedObject(authoring))
            {
                settings.FindProperty("emission.rate").floatValue = 64;
                settings.FindProperty("initialVelocity.speed").floatValue = 0;
                settings.FindProperty("lifetime.lifetime").floatValue = 10;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Color[] CaptureAudioPreview(FluxParticleBuildResult build, int samples, bool reference, string label)
        {
            var cameraRoot = new GameObject("Audio channels preview camera");
            var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBFloat) { antiAliasing = samples };
            var image = new Texture2D(128, 128, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0, 0, -2);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 31;
                camera.targetTexture = rt;
                using (var preview = new FluxParticleEditorPreview(build, authoring, 64))
                {
                    var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                        .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                    for (var frame = 1; frame <= 6; frame++)
                    {
                        var bands = frame == 4 || frame == 5 ? Vector4.zero : new Vector4(0.1f, 0.3f, 0.6f, 0.9f);
                        if (reference)
                        {
                            // Frozen pre-extension sampling formula; average raw bands before nonlinear shaping.
                            var average = 0f;
                            for (var band = 0; band < 4; band++) average += bands[band] * 0.25f;
                            var selected = Mathf.Clamp01((bands.x - 0.02f) / 0.98f);
                            var allBands = Mathf.Clamp01((average - 0.02f) / 0.98f * 1.5f);
                            session.SetUniformVector(FluxParticleAudioInputOutputs.Sample, new Vector4(selected, allBands, frame, 0));
                        }
                        else
                        {
                            session.SetUniformVector(FluxParticleAudioInputOutputs.RawBands, bands);
                            session.SetUniformFloat(FluxParticleAudioInputOutputs.Frame, frame);
                        }
                        preview.Step(0.1f);
                    }
                    preview.Draw(camera, Matrix4x4.identity, 31);
                    camera.Render();
                    RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                    Directory.CreateDirectory("Temp/AudioChannels");
                    File.WriteAllBytes("Temp/AudioChannels/Preview-msaa" + samples + "-" + label + ".exr", image.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
                    return image.GetPixels();
                }
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(rt);
            }
        }
    }
}
