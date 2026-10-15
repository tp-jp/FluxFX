using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Udon.Parameters;
using TpLab.Flux.Udon;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed partial class FluxParticleExecutorTests
    {
        static FluxParticleTrailsModule CreateTrails(int maximum, int points, float duration)
        {
            var module = new FluxParticleTrailsModule();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FluxParticleTrailsModule).GetField("maxTrails", flags).SetValue(module, maximum);
            typeof(FluxParticleTrailsModule).GetField("pointsPerTrail", flags).SetValue(module, points);
            typeof(FluxParticleTrailsModule).GetField("duration", flags).SetValue(module, duration);
            return module;
        }

        [Test]
        public void TrailsLegacySettingsReceiveDefaultsWithoutOverwritingCurrentSettings()
        {
            var module = (FluxParticleTrailsModule)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(FluxParticleTrailsModule));
            module.OnAfterDeserialize();
            Assert.AreEqual(4096, module.MaxTrails);
            Assert.AreEqual(16, module.PointsPerTrail);
            Assert.AreEqual(0.5f, module.Duration);
            var current = CreateTrails(2, 3, 1);
            current.OnAfterDeserialize();
            Assert.AreEqual(2, current.MaxTrails);
            Assert.AreEqual(3, current.PointsPerTrail);
            Assert.AreEqual(1, current.Duration);
        }

        [TestCase(4, FluxParticleCapacity.MaximumExactCount, 0.5f)]
        [TestCase(0, 16, 0.5f)]
        [TestCase(4, 1, 0.5f)]
        [TestCase(4, 16, 0f)]
        [TestCase(4, 16, float.NaN)]
        [TestCase(4, 16, float.PositiveInfinity)]
        [TestCase(4, 16, float.Epsilon)]
        public void TrailsRejectInvalidConfiguration(int maximum, int points, float duration)
        {
            SetModules(CreateTrails(maximum, points, duration));
            Assert.Throws<InvalidOperationException>(() => FluxParticleBuildPipeline.Build(authoring));
        }

        [TestCase(3, 2, 7)]
        [TestCase(20, 8, 7)]
        [TestCase(5, 32, 11)]
        [TestCase(16384, 2, 16385)]
        public void TrailsConfiguredCapacityAndPointsControlAllocationAndSampling(int maximum, int points, int capacity)
        {
            RequireGraphics();
            const float duration = 0.6f;
            SetModules(CreateTrails(maximum, points, duration));
            var build = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(build, capacity);
            Assert.IsTrue(runtime.Initialize(capacity));
            var tracked = Math.Min(maximum, capacity);
            var buffers = (FluxBuffer[])runtime.GetProgramVariable("trailBuffers");
            var renderers = (MeshRenderer[])runtime.GetProgramVariable("trailRenderers");
            Assert.AreEqual(points + 2, buffers.Length);
            Assert.IsTrue(buffers.All(buffer => buffer.Count == tracked));
            Assert.AreEqual(points - 1, renderers.Length);
            var mesh = renderers[0].GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(tracked * 4, mesh.vertexCount);
            Assert.AreEqual(tracked * 4 > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16, mesh.indexFormat);
            runtime.SetVector("lifetime.range", new Vector4(100, 100, 0, 0));
            runtime.Step(0, 0, capacity, 0);
            var last = tracked - 1;
            var slot = last * (capacity / tracked) + Math.Min(last, capacity % tracked);
            Assert.Less(Vector3.Distance((Vector4)ReadTexture(runtime.GetTrailPoint(0), last), ReadValue(runtime, build, "particle.position", slot)), 0.0001f);
            Assert.IsNull(runtime.GetTrailPoint(points));
            var state = (float[])runtime.GetProgramVariable("_trailState");
            runtime.Step(duration / (points - 1) * 0.4f, 0, 0, 0);
            Assert.AreEqual(1, state[FluxParticleTrailHistory.HeadIndex]);
            for (var i = 0; i < points + 1; i++)
            {
                runtime.Step(duration / (points - 1) * 1.1f, 0, 0, 0);
            }
            for (var i = 0; i < points; i++)
            {
                Assert.AreEqual(1, ReadTexture(runtime.GetTrailPoint(i), last).a);
            }
        }

        [Test]
        public void TrailsDurationChangesClearOnlyHistoryAndDeferWhileOff()
        {
            RequireGraphics();
            SetModules(CreateTrails(3, 8, 0.5f));
            var build = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(build);
            var handle = runtime.GetComponentInParent<FluxParticleSystem>().Modules.Trails;
            Assert.IsTrue(runtime.Initialize(4));
            runtime.Step(0, 0, 4, 0);
            runtime.Step(0.1f, 0, 0, 0);
            var age = ReadValue(runtime, build, "particle.age", 0);
            Assert.AreEqual(0.5f, handle.GetDuration());
            Assert.IsFalse(handle.SetDuration(0));
            Assert.IsFalse(handle.SetDuration(float.NaN));
            Assert.IsFalse(runtime.SetFloat("trails.duration", -1));
            Assert.IsTrue(handle.SetDuration(1));
            Assert.AreEqual(1, handle.GetDuration());
            AssertVector(age, ReadValue(runtime, build, "particle.age", 0));
            for (var i = 0; i < 8; i++)
            {
                Assert.AreEqual(-1, ReadTexture(runtime.GetTrailPoint(i), 0).a);
            }
            runtime.Step(0, 0, 0, 0);
            var first = ReadTexture(runtime.GetTrailPoint(0), 0);
            Assert.IsTrue(handle.SetDuration(1));
            Assert.AreEqual(first, ReadTexture(runtime.GetTrailPoint(0), 0));
            handle.SetEnabled(false);
            var state = (float[])((float[])runtime.GetProgramVariable("_trailState")).Clone();
            Assert.IsTrue(handle.SetDuration(0.25f));
            runtime.Step(0.2f, 0, 0, 0);
            CollectionAssert.AreEqual(state, (float[])runtime.GetProgramVariable("_trailState"));
            Assert.AreEqual(first, ReadTexture(runtime.GetTrailPoint(0), 0));
            handle.SetEnabled(true);
            Assert.AreEqual(-1, ReadTexture(runtime.GetTrailPoint(0), 0).a);
            runtime.Step(0, 0, 0, 0);
            runtime.Step(0.04f, 0, 0, 0);
            Assert.AreEqual(1, ReadTexture(runtime.GetTrailPoint(1), 0).a);
            Assert.AreEqual(0.25f, ((Material[])runtime.GetProgramVariable("trailMaterials"))[0].GetFloat("_TrailDuration"));
        }

        [Test]
        public void TrailsPreviewRebuildResetsChangedLayoutOrDurationAndPreservesParticles()
        {
            RequireGraphics();
            SetModules(CreateTrails(3, 8, 0.5f));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var preview = new FluxParticleEditorPreview(FluxParticleBuildPipeline.Build(authoring), authoring, 4))
            {
                preview.Step(0.25f);
                preview.Step(0.25f);
                SetModules(CreateTrails(2, 5, 1));
                using (var replacement = preview.Recreate(FluxParticleBuildPipeline.Build(authoring)))
                {
                    Assert.AreEqual(preview.PlaybackTime, replacement.PlaybackTime);
                    var buffers = (FluxBuffer[])typeof(FluxParticleEditorPreview).GetField("trailBuffers", flags).GetValue(replacement);
                    Assert.AreEqual(7, buffers.Length);
                    Assert.IsTrue(buffers.All(buffer => buffer.Count == 2 && ReadTexture(buffer.Texture, 0).a == -1));
                    replacement.Step(0);
                    var state = (float[])typeof(FluxParticleEditorPreview).GetField("trailState", flags).GetValue(replacement);
                    Assert.AreEqual(1, ReadTexture(buffers[(int)state[FluxParticleTrailHistory.MetadataIndex]].Texture, 0).g);
                    typeof(FluxParticleTrailsModule).GetField("duration", flags).SetValue(authoring.Modules[0], 0.25f);
                    replacement.Step(0);
                    Assert.AreEqual(1, state[FluxParticleTrailHistory.TickIndex]);
                    Assert.AreEqual(1, buffers.Take(5).Count(buffer => ReadTexture(buffer.Texture, 0).a >= 0));
                }
            }
        }

        [TestCase(FluxParticleSimulationSpace.Local)]
        [TestCase(FluxParticleSimulationSpace.World)]
        public void TrailsRecordWorldPositionsAndPreserveOldPoints(FluxParticleSimulationSpace space)
        {
            RequireGraphics();
            SetModules(new FluxParticleTrailsModule());
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("simulationSpace").intValue = (int)space;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            authoring.transform.SetPositionAndRotation(new Vector3(7, 3, 2), Quaternion.Euler(0, 0, 30));
            authoring.transform.localScale = new Vector3(2, 3, 1);
            var build = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(build);
            Assert.IsTrue(runtime.Initialize(4));
            runtime.SetFloat("spawn.speed", 0);
            runtime.SetVector("spawn.position", new Vector4(1, 2, 3, 0));
            runtime.Step(0, 0, 1, 0);
            var statePosition = (Vector3)ReadValue(runtime, build, "particle.position", 0);
            var expected = space == FluxParticleSimulationSpace.World ? statePosition : authoring.transform.TransformPoint(statePosition);
            var first = (Vector4)ReadTexture(runtime.GetTrailPoint(0), 0);
            Assert.Less(Vector3.Distance(expected, first), 0.0001f);
            var generation = ReadTexture(runtime.TrailMetadata, 0);
            Assert.AreEqual(1, generation.g);
            Assert.AreEqual(generation.r, first.w);
            authoring.transform.position += Vector3.right * 10;
            runtime.Step(0.04f, 0, 0, 0);
            AssertVector(first, ReadTexture(runtime.GetTrailPoint(1), 0));
            var next = (Vector4)ReadTexture(runtime.GetTrailPoint(0), 0);
            Assert.AreEqual(space == FluxParticleSimulationSpace.Local ? first.x + 10 : first.x, next.x, 0.0001f);
            Assert.AreEqual(first.w, next.w);
        }

        [Test]
        public void TrailsInvalidateDeathAndSameKeyReuseBetweenSamples()
        {
            RequireGraphics();
            SetModules(new FluxParticleTrailsModule());
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            Assert.IsTrue(runtime.Initialize(4));
            runtime.SetVector("lifetime.range", new Vector4(0.01f, 0.01f, 0, 0));
            runtime.Step(0, 0, 1, 17);
            var oldPoint = ReadTexture(runtime.GetTrailPoint(0), 0);
            // 記録間隔内で死亡・再生成しても、SpawnKeyは再利用し得る。
            runtime.Step(0.011f, 0, 0, 0);
            Assert.AreEqual(0, ReadTexture(runtime.TrailMetadata, 0).g);
            runtime.Step(0.005f, 0, 1, 17);
            var generation = ReadTexture(runtime.TrailMetadata, 0);
            Assert.AreEqual(1, generation.g);
            Assert.AreNotEqual(oldPoint.a, generation.r);
            Assert.AreEqual(oldPoint, ReadTexture(runtime.GetTrailPoint(0), 0), "Subsample spawn must not fabricate another position sample.");
            runtime.Step(0.011f, 0, 0, 0);
            Assert.AreEqual(0, ReadTexture(runtime.TrailMetadata, 0).g, "Death hides every historical segment immediately.");
            runtime.Clear();
            for (var i = 0; i < 16; i++)
            {
                Assert.AreEqual(-1, ReadTexture(runtime.GetTrailPoint(i), 0).a);
            }
        }

        [TestCase(4096)]
        [TestCase(8193)]
        public void TrailsUseBounded4096By16HistoryAndWrapRing(int capacity)
        {
            RequireGraphics();
            SetModules(new FluxParticleTrailsModule());
            var build = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(build, capacity);
            Assert.IsTrue(runtime.Initialize(capacity));
            runtime.SetVector("lifetime.range", new Vector4(10, 10, 0, 0));
            runtime.Step(0, 0, capacity, 0);
            Assert.AreEqual(64, runtime.GetTrailPoint(0).width);
            Assert.AreEqual(64, runtime.TrailMetadata.width);
            var last = 4095;
            var slot = last * (capacity / 4096) + Math.Min(last, capacity % 4096);
            var point = (Vector4)ReadTexture(runtime.GetTrailPoint(0), last);
            Assert.Less(Vector3.Distance(point, ReadValue(runtime, build, "particle.position", slot)), 0.0001f);
            Assert.AreEqual(1, ReadTexture(runtime.TrailMetadata, last).g);
            for (var step = 0; step < 18; step++)
            {
                runtime.Step(0.04f, 0, 0, 0);
            }
            for (var i = 0; i < 16; i++)
            {
                Assert.AreEqual(point.w, ReadTexture(runtime.GetTrailPoint(i), last).a);
            }
            var materials = (Material[])runtime.GetProgramVariable("trailMaterials");
            Assert.AreEqual(15, materials.Length);
            Assert.AreEqual(runtime.GetTrailPoint(0), materials[14].GetTexture("_TrailSecond"));
            Assert.AreEqual(runtime.GetTrailPoint(15), materials[0].GetTexture("_TrailFirst"));
            var meshes = runtime.transform.Find("__FluxFXGenerated/Trails").GetComponentsInChildren<MeshFilter>();
            Assert.AreEqual(15, meshes.Length);
            Assert.AreEqual(4096 * 4, meshes[0].sharedMesh.vertexCount);
            Assert.IsTrue(meshes.All(mesh => mesh.sharedMesh == meshes[0].sharedMesh));
            Assert.IsTrue(AssetDatabase.Contains(meshes[0].sharedMesh));
        }

        [Test]
        public void TrailsOffClearOverflowAndReinitializeDiscardHistory()
        {
            RequireGraphics();
            SetModules(new FluxParticleTrailsModule());
            var build = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(build);
            var handle = runtime.GetComponentInParent<FluxParticleSystem>().Modules.Trails;
            Assert.IsNotNull(handle);
            Assert.IsTrue(handle.SetEnabled(false));
            Assert.IsTrue(runtime.Initialize(4));
            var renderers = (MeshRenderer[])runtime.GetProgramVariable("trailRenderers");
            Assert.IsTrue(renderers.All(renderer => !renderer.enabled));
            runtime.Step(0, 0, 1, 0);
            Assert.AreEqual(0, ReadTexture(runtime.TrailMetadata, 0).g);
            Assert.AreEqual(0, ((float[])runtime.GetProgramVariable("_trailState"))[FluxParticleTrailHistory.TickIndex]);
            handle.SetEnabled(true);
            handle.SetWidth(0.2f);
            handle.SetColor(Color.red);
            runtime.Step(0.04f, 0, 0, 0);
            Assert.AreEqual(1, ReadTexture(runtime.TrailMetadata, 0).g);
            var state = (float[])runtime.GetProgramVariable("_trailState");
            state[FluxParticleTrailHistory.TickIndex] = FluxParticleTrailHistory.MaximumTick;
            runtime.Step(0.04f, 0, 0, 0);
            Assert.AreEqual(1, ReadTexture(runtime.TrailMetadata, 0).r);
            Assert.AreEqual(-1, ReadTexture(runtime.GetTrailPoint(1), 0).a);
            var oldTexture = runtime.GetTrailPoint(0);
            runtime.Release();
            Assert.IsTrue(oldTexture == null || !oldTexture.IsCreated());
            Assert.IsNull(runtime.TrailMetadata);
            Assert.IsTrue(runtime.Initialize(4));
            Assert.AreEqual(0, ReadTexture(runtime.TrailMetadata, 0).g);
            Assert.AreEqual(-1, ReadTexture(runtime.GetTrailPoint(0), 0).a);
        }

        [Test]
        public void TrailsPreviewRecreatesHistoryAndRestartClearsIt()
        {
            RequireGraphics();
            SetModules(new FluxParticleTrailsModule());
            var build = FluxParticleBuildPipeline.Build(authoring);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var preview = new FluxParticleEditorPreview(build, authoring, 4096))
            {
                preview.Step(0.25f);
                preview.Step(0.25f);
                var buffers = (FluxBuffer[])typeof(FluxParticleEditorPreview).GetField("trailBuffers", flags).GetValue(preview);
                var state = (float[])typeof(FluxParticleEditorPreview).GetField("trailState", flags).GetValue(preview);
                Assert.AreEqual(18, buffers.Length);
                Assert.AreEqual(64, buffers[0].Texture.width);
                Assert.AreEqual(1, ReadTexture(buffers[(int)state[2]].Texture, 0).g);
                using (var replacement = preview.Recreate(build))
                {
                    var copied = (FluxBuffer[])typeof(FluxParticleEditorPreview).GetField("trailBuffers", flags).GetValue(replacement);
                    for (var i = 0; i < buffers.Length; i++)
                    {
                        Assert.AreEqual(ReadTexture(buffers[i].Texture, 0), ReadTexture(copied[i].Texture, 0));
                    }
                    replacement.Restart();
                    Assert.AreEqual(-1, ReadTexture(copied[0].Texture, 0).a);
                    Assert.AreEqual(0, ReadTexture(copied[16].Texture, 0).g);
                }
            }
        }

        [Test]
        public void TrailsPreviewOffFreezesHistoryAndResumeDiscardsOldPoints()
        {
            RequireGraphics();
            var module = new FluxParticleTrailsModule();
            SetModules(module);
            var build = FluxParticleBuildPipeline.Build(authoring);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var enabled = typeof(FluxParticleModule).GetField("enabled", flags);
            using (var preview = new FluxParticleEditorPreview(build, authoring, 4))
            {
                preview.Step(0.25f);
                preview.Step(0.25f);
                var buffers = (FluxBuffer[])typeof(FluxParticleEditorPreview).GetField("trailBuffers", flags).GetValue(preview);
                var state = (float[])typeof(FluxParticleEditorPreview).GetField("trailState", flags).GetValue(preview);
                var oldState = (float[])state.Clone();
                var oldHistory = buffers.Select(buffer => ReadTexture(buffer.Texture, 0)).ToArray();
                enabled.SetValue(module, false);
                preview.Step(0.25f);
                Assert.AreEqual(false, typeof(FluxParticleEditorPreview).GetField("trailsEnabled", flags).GetValue(preview));
                CollectionAssert.AreEqual(oldState, state);
                CollectionAssert.AreEqual(oldHistory, buffers.Select(buffer => ReadTexture(buffer.Texture, 0)).ToArray());
                using (var disabled = preview.Recreate(FluxParticleBuildPipeline.Build(authoring)))
                {
                    var copied = (FluxBuffer[])typeof(FluxParticleEditorPreview).GetField("trailBuffers", flags).GetValue(disabled);
                    Assert.IsTrue(copied.All(buffer => ReadTexture(buffer.Texture, 0).a == -1));
                }
                enabled.SetValue(module, true);
                preview.Step(0);
                Assert.AreEqual(1, state[FluxParticleTrailHistory.TickIndex]);
                Assert.AreEqual(1, ReadTexture(buffers[(int)state[FluxParticleTrailHistory.MetadataIndex]].Texture, 0).g);
                Assert.AreEqual(1, buffers.Take(16).Count(buffer => ReadTexture(buffer.Texture, 0).a >= 0));
                preview.Step(0.04f);
                Assert.AreEqual(2, buffers.Take(16).Count(buffer => ReadTexture(buffer.Texture, 0).a >= 0));
                enabled.SetValue(module, false);
                // Inspectorの再生成がStepより先でも、OFF後に古い履歴を引き継がない。
                using (var disabled = preview.Recreate(FluxParticleBuildPipeline.Build(authoring)))
                {
                    enabled.SetValue(module, true);
                    using (var resumed = disabled.Recreate(build))
                    {
                        var copied = (FluxBuffer[])typeof(FluxParticleEditorPreview).GetField("trailBuffers", flags).GetValue(resumed);
                        Assert.IsTrue(copied.All(buffer => ReadTexture(buffer.Texture, 0).a == -1));
                    }
                }
            }
        }

        [Test]
        public void TrailsDurationClipsOldPositionsAfterSlowStep()
        {
            RequireGraphics();
            SetModules(CreateTrails(1, 2, 0.1f));
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            Assert.IsTrue(runtime.Initialize(4));
            runtime.SetFloat("spawn.speed", 0);
            runtime.SetFloat("trails.width", 1);
            runtime.Step(0, 0, 1, 0);
            authoring.transform.position = Vector3.right;
            runtime.Step(0.2f, 0, 0, 0);
            Assert.AreEqual(0, ReadTexture(runtime.GetTrailPoint(1), 0).r, "Old history must remain available for the clipping check.");
            var segment = ((MeshRenderer[])runtime.GetProgramVariable("trailRenderers"))[0];
            segment.gameObject.layer = 31;
            var cameraObject = new GameObject("Trail Duration Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.position = new Vector3(0.5f, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 31;
            var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGBFloat);
            target.Create();
            camera.targetTexture = target;
            try
            {
                camera.Render();
                Assert.AreEqual(0, ReadTexture(target, 32 * 64 + 24).a, "The expired half of the segment must be clipped.");
                Assert.Greater(ReadTexture(target, 32 * 64 + 42).a, 0.1f, "The recent half must remain visible.");
                runtime.Step(0.11f, 0, 0, 0);
                camera.Render();
                Assert.AreEqual(0, ReadTexture(target, 32 * 64 + 42).a, "Stationary samples must not retain the old segment.");
            }
            finally
            {
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void TrailsRenderVisibleSegmentsAndClipReusedGeneration()
        {
            RequireGraphics();
            SetModules(new FluxParticleTrailsModule());
            var runtime = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            Assert.IsTrue(runtime.Initialize(4));
            runtime.SetFloat("spawn.speed", 0);
            runtime.SetFloat("trails.width", 1);
            runtime.SetVector("lifetime.range", new Vector4(1, 1, 0, 0));
            runtime.Step(0, 0, 1, 0);
            authoring.transform.position = Vector3.right;
            runtime.Step(0.04f, 0, 0, 0);
            var segments = runtime.transform.Find("__FluxFXGenerated/Trails").GetComponentsInChildren<MeshRenderer>();
            foreach (var segment in segments)
            {
                segment.gameObject.layer = 31;
            }
            var cameraObject = new GameObject("Trail Render Test Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.position = new Vector3(0.5f, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 2;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 31;
            var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGBFloat);
            target.Create();
            camera.targetTexture = target;
            try
            {
                camera.Render();
                Assert.Greater(ReadTexture(target, 32 * 64 + 32).a, 0.1f);
                var handle = runtime.GetComponentInParent<FluxParticleSystem>().Modules.Trails;
                var buffers = (FluxBuffer[])runtime.GetProgramVariable("trailBuffers");
                var oldHistory = buffers.Select(buffer => ReadTexture(buffer.Texture, 0)).ToArray();
                var oldState = (float[])((float[])runtime.GetProgramVariable("_trailState")).Clone();
                Assert.IsTrue(handle.SetEnabled(false));
                Assert.IsTrue(segments.All(segment => !segment.enabled));
                authoring.transform.position = Vector3.right * 2;
                cameraObject.transform.position = new Vector3(0.5f, 0, -10);
                runtime.Step(0.1f, 0, 0, 0);
                CollectionAssert.AreEqual(oldState, (float[])runtime.GetProgramVariable("_trailState"));
                CollectionAssert.AreEqual(oldHistory, buffers.Select(buffer => ReadTexture(buffer.Texture, 0)).ToArray());
                camera.Render();
                Assert.AreEqual(0, ReadTexture(target, 32 * 64 + 32).a);
                Assert.IsTrue(handle.SetEnabled(true));
                Assert.IsTrue(segments.All(segment => segment.enabled));
                for (var i = 0; i < 16; i++)
                {
                    Assert.AreEqual(-1, ReadTexture(runtime.GetTrailPoint(i), 0).a);
                }
                runtime.Step(0, 0, 0, 0);
                camera.Render();
                Assert.AreEqual(0, ReadTexture(target, 32 * 64 + 32).a, "Re-enabling must not connect to the old position.");
                var first = ReadTexture(runtime.GetTrailPoint(0), 0);
                Assert.IsTrue(handle.SetEnabled(true));
                Assert.AreEqual(first, ReadTexture(runtime.GetTrailPoint(0), 0), "Setting ON again must preserve the new history.");
                authoring.transform.position = Vector3.zero;
                cameraObject.transform.position = new Vector3(0.5f, 0, -10);
                runtime.Step(0.04f, 0, 0, 0);
                camera.Render();
                Assert.Greater(ReadTexture(target, 32 * 64 + 32).a, 0.1f, "New segments must render after resuming.");
                // 新しいSlotの最初の点だけでは、以前の軌跡と線を作らない。
                runtime.Step(1, 0, 0, 0);
                runtime.Step(0, 0, 1, 0);
                camera.Render();
                Assert.AreEqual(0, ReadTexture(target, 32 * 64 + 32).a);
            }
            finally
            {
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
        [UnityTest]
        [Explicit]
        [Category("UdonRuntime")]
        public IEnumerator TrailsExecuteOnUdonVmAndInvalidateReuse()
        {
            RequireGraphics();
            Object.DestroyImmediate(root);
            yield return new EnterPlayMode();
            root = new GameObject("Module Handle Udon VM Test");
            authoring = root.AddComponent<FluxParticleAuthoring>();
            folder = "Assets/FluxFX/Tests/ParticleExecutor_" + Guid.NewGuid().ToString("N");
            generationSession = FluxParticleGenerationSession.Begin("Test", folder);
            SetModules(CreateTrails(3, 8, 0.5f));
            var compiled = FluxParticleBuildPipeline.Build(authoring);
            var runtime = CreateRuntime(compiled);
            SetModules();
            var empty = CreateRuntime(FluxParticleBuildPipeline.Build(authoring));
            var probePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FluxFX/Tests/ModuleHandleProbe.prefab");
            Assert.IsNotNull(probePrefab);
            var probeObject = (GameObject)PrefabUtility.InstantiatePrefab(probePrefab);
            probeObject.transform.SetParent(root.transform, false);
            var probeProxy = probeObject.GetComponent<UdonSharp.UdonSharpBehaviour>();
            probeProxy.SetProgramVariable("target", runtime.GetComponentInParent<FluxParticleSystem>());
            probeProxy.SetProgramVariable("empty", empty.GetComponentInParent<FluxParticleSystem>());
            var binding = compiled.Shaders.Compilation.StateLayout.Pages.SelectMany(page => page.Bindings)
                .Single(value => value.ValueId == "particle.velocity");
            probeProxy.SetProgramVariable("resultPage", binding.PageId);
            GameObject managerObject = null;
            try
            {
                if (!VRC.Udon.UdonManager.HasInstance)
                {
                    managerObject = new GameObject("Module Handle Test Udon Manager");
                    managerObject.AddComponent<VRC.Udon.UdonManager>().Awake();
                }
                foreach (var backing in root.GetComponentsInChildren<VRC.Udon.UdonBehaviour>(true))
                {
                    backing.InitializeUdonContent();
                }
                probeProxy.SetProgramVariable("target", runtime.GetComponentInParent<FluxParticleSystem>());
                probeProxy.SetProgramVariable("empty", empty.GetComponentInParent<FluxParticleSystem>());
                probeProxy.SetProgramVariable("resultPage", binding.PageId);
                foreach (var proxy in root.GetComponentsInChildren<UdonSharp.UdonSharpBehaviour>(true))
                {
                    UdonSharpEditorUtility.CopyProxyToUdon(proxy);
                }
                var probe = UdonSharpEditorUtility.GetBackingUdonBehaviour(probeProxy);
                Assert.AreEqual(UdonSharpEditorUtility.GetBackingUdonBehaviour(runtime.GetComponentInParent<FluxParticleSystem>()), probe.GetProgramVariable("target"));
                probe.SendCustomEvent("VerifyTrails");
                Assert.AreEqual(true, probe.GetProgramVariable("passed"), "Compiled Udon probe must complete all handle checks.");
                var oldPoint = ReadTexture((RenderTexture)probe.GetProgramVariable("trailOldPoint"), 0);
                var metadata = ReadTexture((RenderTexture)probe.GetProgramVariable("resultTexture"), 0);
                Assert.AreEqual(1, metadata.g);
                Assert.AreNotEqual(oldPoint.a, metadata.r, "Udon VM must invalidate old history even with a repeated SpawnKey.");
                var runtimeBacking = UdonSharpEditorUtility.GetBackingUdonBehaviour(runtime);
                var state = (float[])((float[])runtimeBacking.GetProgramVariable("_trailState")).Clone();
                var newest = (RenderTexture)probe.GetProgramVariable("trailOldPoint");
                var before = ReadTexture(newest, 0);
                var renderers = runtime.transform.Find("__FluxFXGenerated/Trails").GetComponentsInChildren<MeshRenderer>();
                probe.SendCustomEvent("VerifyTrailsOff");
                Assert.AreEqual(true, probe.GetProgramVariable("passed"));
                Assert.IsTrue(renderers.All(renderer => !renderer.enabled));
                CollectionAssert.AreEqual(state, (float[])runtimeBacking.GetProgramVariable("_trailState"));
                Assert.AreEqual(before, ReadTexture(newest, 0));
                probe.SendCustomEvent("VerifyTrailsResume");
                Assert.AreEqual(true, probe.GetProgramVariable("passed"));
                Assert.IsTrue(renderers.All(renderer => renderer.enabled));
                Assert.AreEqual(-1, ReadTexture((RenderTexture)probe.GetProgramVariable("trailOldPoint"), 0).a);
                Assert.AreEqual(1, ReadTexture((RenderTexture)probe.GetProgramVariable("resultTexture"), 0).g);
                Assert.AreEqual(1, ((float[])runtimeBacking.GetProgramVariable("_trailState"))[FluxParticleTrailHistory.TickIndex]);
                probe.SendCustomEvent("VerifyTrailsDuration");
                Assert.AreEqual(true, probe.GetProgramVariable("passed"));
                Assert.AreEqual(-1, ReadTexture((RenderTexture)probe.GetProgramVariable("trailOldPoint"), 0).a);
                Assert.AreEqual(1, ReadTexture((RenderTexture)probe.GetProgramVariable("resultTexture"), 0).g);
                Assert.AreEqual(1, ((float[])runtimeBacking.GetProgramVariable("_trailState"))[FluxParticleTrailHistory.TickIndex]);

            }
            finally
            {
                foreach (var buffer in root.GetComponentsInChildren<FluxBuffer>(true))
                {
                    var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(buffer);
                    var texture = backing.GetProgramVariable("_texture") as RenderTexture;
                    if (texture == null) continue;
                    texture.Release();
                    Object.DestroyImmediate(texture);
                }
                if (managerObject != null) Object.DestroyImmediate(managerObject);
            }
        }

    }
}
