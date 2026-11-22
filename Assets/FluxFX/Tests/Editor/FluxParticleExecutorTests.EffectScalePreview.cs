using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        [TestCase(FluxParticleRenderMode.Billboard, 1)]
        [TestCase(FluxParticleRenderMode.Billboard, 4)]
        [TestCase(FluxParticleRenderMode.Mesh, 1)]
        public void EffectScalePreviewMatchesScaledCameraAndPreservesHdr(FluxParticleRenderMode mode, int samples)
        {
            RequireGraphics();
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var fade = new FluxParticleDistanceFadeModule();
                JsonUtility.FromJsonOverwrite("{\"startDistance\":2,\"endDistance\":4}", fade);
                SetModules(new FluxParticleGravityModule(), fade, CreateTrails(64, 8, 0.5f));
                using (var settings = new SerializedObject(authoring))
                {
                    settings.FindProperty("render.mode").intValue = (int)mode;
                    settings.FindProperty("render.mesh").objectReferenceValue = cube.GetComponent<MeshFilter>().sharedMesh;
                    settings.FindProperty("render.startSize").floatValue = mode == FluxParticleRenderMode.Mesh ? 0.1f : 1;
                    settings.FindProperty("render.intensity").floatValue = 4;
                    settings.FindProperty("render.blendMode").intValue = (int)FluxParticleBlendMode.Additive;
                    settings.FindProperty("emission.rate").floatValue = 128;
                    settings.FindProperty("shape.radius").floatValue = 0.3f;
                    settings.FindProperty("shape.type").intValue = (int)FluxParticleShapeType.Sphere;
                    settings.FindProperty("initialVelocity.speed").floatValue = 0.1f;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                var baseline = CaptureEffectScalePreview(1, samples, mode);
                var expanded = CaptureEffectScalePreview(200, samples, mode);
                double difference = 0, energy = 0;
                var max = 0f; var peak = 0f;
                for (var i = 0; i < baseline.Length; i++)
                {
                    for (var c = 0; c < 4; c++)
                    {
                        var delta = Mathf.Abs(baseline[i][c] - expanded[i][c]);
                        difference += delta; energy += Mathf.Abs(baseline[i][c]); max = Mathf.Max(max, delta);
                    }
                    peak = Mathf.Max(peak, baseline[i].maxColorComponent);
                }
                var relative = difference / Math.Max(energy, 1e-12);
                Assert.Greater(peak, 1, "HDR output must survive display scaling");
                Assert.Less(relative, 0.002, "Scaled camera should yield the same particle and trail image");
                var directory = Environment.GetEnvironmentVariable("FLUXFX_TEST_OUTPUT");
                if (!string.IsNullOrEmpty(directory)) File.WriteAllText(Path.Combine(directory, "preview-" + mode + "-msaa" + samples + ".txt"),
                    "Relative L1=" + relative.ToString("R") + "; maximum=" + max.ToString("R") + "; peak=" + peak.ToString("R"));
            }
            finally { Object.DestroyImmediate(cube); }
        }

        Color[] CaptureEffectScalePreview(float scale, int samples, FluxParticleRenderMode mode)
        {
            Assert.IsTrue(authoring.SetEffectScale(scale));
            var build = FluxParticleBuildPipeline.Build(authoring);
            var cameraRoot = new GameObject("Effect Scale preview camera");
            var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGBFloat) { antiAliasing = samples };
            var image = new Texture2D(256, 256, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0, -0.25f, -3) * scale;
                camera.nearClipPlane = 0.01f * scale;
                camera.farClipPlane = 20 * scale;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 31;
                camera.allowHDR = true;
                camera.allowMSAA = samples > 1;
                camera.targetTexture = target;
                using var preview = new FluxParticleEditorPreview(build, authoring, 64);
                Assert.AreEqual(scale, preview.EffectScale);
                for (var frame = 0; frame < 8; frame++) preview.Step(0.1f);
                preview.Draw(camera, Matrix4x4.identity, 31);
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
                var directory = Environment.GetEnvironmentVariable("FLUXFX_TEST_OUTPUT");
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    var label = mode + "-msaa" + samples + "-scale" + scale;
                    File.WriteAllBytes(Path.Combine(directory, "preview-" + label + ".exr"), image.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
                    File.WriteAllBytes(Path.Combine(directory, "preview-" + label + ".png"), image.EncodeToPNG());
                }
                return image.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(cameraRoot); Object.DestroyImmediate(image); Object.DestroyImmediate(target);
            }
        }
    }
}
