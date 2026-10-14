using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleEditorPreviewTests
    {
        [Test]
        public void InspectorReenableRecreatesPreviewWithUnchangedAuthoring()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            UnityEditor.Editor editor = null;
            try
            {
                var system = root.GetComponent<FluxParticleSystem>();
                if (system.GetComponent<FluxParticleAuthoring>() == null)
                {
                    system.gameObject.AddComponent<FluxParticleAuthoring>();
                }
                editor = UnityEditor.Editor.CreateEditor(system, typeof(FluxParticleSystemEditor));
                var type = typeof(FluxParticleSystemEditor);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var ensure = type.GetMethod("EnsureParticlePreview", flags);
                var previewField = type.GetField("_particlePreview", flags);
                ensure.Invoke(editor, null);
                var first = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.IsNotNull(first, (string)type.GetField("_backendPreviewError", flags).GetValue(editor));
                first.Step(0.25f);
                ensure.Invoke(editor, null);
                Assert.AreSame(first, previewField.GetValue(editor));
                type.GetField("_previewPlaying", flags).SetValue(editor, false);
                type.GetMethod("OnDisable", flags).Invoke(editor, null);
                type.GetMethod("OnEnable", flags).Invoke(editor, null);
                ensure.Invoke(editor, null);
                var second = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.IsNotNull(second, (string)type.GetField("_backendPreviewError", flags).GetValue(editor));
                Assert.AreNotSame(first, second);
                Assert.IsFalse((bool)type.GetField("_previewPlaying", flags).GetValue(editor));
                second.Step(0.25f);
                second.Restart();
                second.Step(0.25f);
                Assert.AreEqual(0.25, second.PlaybackTime, 0.0001);
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(FluxParticleSimulationSpace.Local)]
        [TestCase(FluxParticleSimulationSpace.World)]
        public void PreviewSimulatesRestartsAndReleasesGpuResources(FluxParticleSimulationSpace space)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var root = new GameObject("Editor Preview Test");
            FluxParticleEditorPreview preview = null;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("simulationSpace").intValue = (int)space;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.transform.SetPositionAndRotation(new Vector3(10, 20, 30), Quaternion.Euler(0, 60, 0));
                var build = FluxParticleBuildPipeline.Build(authoring);
                preview = new FluxParticleEditorPreview(build, authoring, 4);
                var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                    .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                var pages = session.CurrentPages.ToArray();
                Assert.AreEqual(4, preview.Count);
                Assert.AreEqual(0, preview.PlaybackTime);
                preview.Step(0.25f);
                Assert.AreEqual(0, ReadValue(session, build, "particle.age").x, 0.0001f);
                var spawnPosition = ReadValue(session, build, "particle.position");
                var spawnVelocity = ReadValue(session, build, "particle.velocity");
                Assert.AreEqual(authoring.InitialVelocity.Speed, spawnVelocity.magnitude, 0.0001f);
                var expectedSpawn = space == FluxParticleSimulationSpace.World ? root.transform.position : Vector3.zero;
                for (var component = 0; component < 3; component++)
                {
                    Assert.AreEqual(expectedSpawn[component], spawnPosition[component], 0.0001f);
                }
                root.transform.position += new Vector3(50, 50, 50);
                preview.Step(0.25f);
                Assert.AreEqual(0.25f, ReadValue(session, build, "particle.age").x, 0.0001f);
                Assert.AreEqual(0.5, preview.PlaybackTime, 0.0001);
                var expectedPosition = spawnPosition + spawnVelocity * 0.25f;
                var actualPosition = ReadValue(session, build, "particle.position");
                for (var component = 0; component < 3; component++)
                {
                    Assert.AreEqual(expectedPosition[component], actualPosition[component], 0.0001f, $"Component {component}");
                }
                preview.Restart();
                Assert.AreEqual(0, preview.PlaybackTime);
                Assert.AreEqual(-1, ReadValue(session, build, "particle.age").x, 0.0001f);
                preview.Step(0.25f);
                Assert.AreEqual(0, ReadValue(session, build, "particle.spawn-key").x, 0.0001f);
                preview.Dispose();
                Assert.IsTrue(pages.All(page => page == null));
                Assert.Throws<ObjectDisposedException>(() => session.Step());
                Assert.DoesNotThrow(() => preview.Dispose());
            }
            finally
            {
                preview?.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator AliveCountTracksGpuStateAndResetsOnRestart()
        {
            if (!SystemInfo.supportsAsyncGPUReadback || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with asynchronous RGBAFloat readback is required.");
            }
            var root = new GameObject("Preview Count Test");
            FluxParticleEditorPreview preview = null;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                preview = new FluxParticleEditorPreview(FluxParticleBuildPipeline.Build(authoring), authoring, 4);
                Assert.AreEqual(0, preview.AliveCount);
                preview.Step(0.25f);
                preview.RequestAliveCount();
                var deadline = EditorApplication.timeSinceStartup + 5;
                while (preview.AliveCount == 0 && EditorApplication.timeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.AreEqual(2, preview.AliveCount);
                preview.Restart();
                Assert.AreEqual(0, preview.PlaybackTime);
                Assert.AreEqual(0, preview.AliveCount);
                preview.RequestAliveCount();
                yield return null;
                Assert.AreEqual(0, preview.AliveCount);
            }
            finally
            {
                preview?.Dispose();
                Object.DestroyImmediate(root);
            }
        }
        [Test]
        public void NoisePreviewReceivesSimulationTime()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var root = new GameObject("Noise Preview Test");
            FluxParticleEditorPreview preview = null;
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(authoring, new System.Collections.Generic.List<TpLab.Flux.FX.Scripts.Modules.FluxParticleModule>
                    {
                        new TpLab.Flux.FX.Scripts.Modules.FluxParticleNoiseModule()
                    });
                var build = FluxParticleBuildPipeline.Build(authoring);
                preview = new FluxParticleEditorPreview(build, authoring, 4);
                var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                    .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                session.SetFloat("spawn.speed", 0);
                preview.Step(0.25f);
                preview.Step(0.25f);
                var velocity = ReadValue(session, build, "particle.velocity");
                Assert.AreEqual(Mathf.Sin(0.25f) * 0.25f, velocity.x, 0.0005f);
                Assert.AreEqual(Mathf.Sin(0.25f * 1.17f) * 0.25f, velocity.y, 0.0005f);
                Assert.AreEqual(Mathf.Sin(0.25f * 0.83f) * 0.25f, velocity.z, 0.0005f);
            }
            finally
            {
                preview?.Dispose();
                Object.DestroyImmediate(root);
            }
        }
        static Vector4 ReadValue(FluxParticleProgramGpuSession session, FluxParticleBuildResult build, string id)
        {
            Assert.IsTrue(build.Shaders.Compilation.StateLayout.TryGetBinding(id, out var binding));
            var page = session.CurrentPages[binding.PageId];
            var texture = new Texture2D(page.width, page.height, TextureFormat.RGBAFloat, false, true);
            var active = RenderTexture.active;
            try
            {
                RenderTexture.active = page;
                texture.ReadPixels(new Rect(0, 0, page.width, page.height), 0, 0);
                texture.Apply();
                var pixel = (Vector4)texture.GetPixel(0, 0);
                var result = Vector4.zero;
                for (var i = 0; i < binding.ComponentCount; i++) result[i] = pixel[binding.ChannelOffset + i];
                return result;
            }
            finally
            {
                RenderTexture.active = active;
                Object.DestroyImmediate(texture);
            }
        }
    }
}
