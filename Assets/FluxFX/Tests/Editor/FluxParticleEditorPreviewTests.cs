using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleEditorPreviewTests
    {
        /// <summary>
        /// 実際のIMGUIイベントでInspectorとPreview操作欄を描画する検証用Windowです。
        /// </summary>
        sealed class PreviewInspectorWindow : EditorWindow
        {
            internal UnityEditor.Editor Inspector;
            internal int DrawCount;
            Vector2 _scroll;

            void OnGUI()
            {
                if (Inspector == null) return;
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                Inspector.OnInspectorGUI();
                typeof(FluxParticleAuthoringEditor).GetMethod("DrawScenePreviewControls", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Inspector, null);
                EditorGUILayout.EndScrollView();
                DrawCount++;
            }
        }

        [UnityTest]
        public IEnumerator StoppedInspectorAndOverlayRepaintWithoutBuildingWithBackendOpenOrClosed()
        {
            const string expandedKey = "TpLab.FluxFX.ParticleSystemInspector.BackendExpanded";
            var wasExpanded = SessionState.GetBool(expandedKey, false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            UnityEditor.Editor editor = null;
            PreviewInspectorWindow window = null;
            try
            {
                var authoring = root.GetComponent<FluxParticleAuthoring>();
                editor = UnityEditor.Editor.CreateEditor(authoring, typeof(FluxParticleAuthoringEditor));
                window = ScriptableObject.CreateInstance<PreviewInspectorWindow>();
                window.Inspector = editor;
                window.ShowUtility();
                var settings = new SerializedObject(authoring);
                var type = typeof(FluxParticleAuthoringEditor);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                for (var i = 0; i < 4; i++)
                {
                    SessionState.SetBool(expandedKey, i % 2 == 0);
                    settings.Update();
                    settings.FindProperty("particleCount").intValue = 128 + i;
                    settings.FindProperty("systemFollow.target").intValue = (int)FluxParticleFollowTarget.LocalPlayerPosition;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    var before = window.DrawCount;
                    var deadline = EditorApplication.timeSinceStartup + 5;
                    while (window.DrawCount == before && EditorApplication.timeSinceStartup < deadline)
                    {
                        window.Repaint();
                        yield return null;
                    }
                    Assert.Greater(window.DrawCount, before, "The real Inspector and overlay GUI must render.");
                    Assert.IsNull(type.GetField("_particlePreview", flags).GetValue(editor));
                    Assert.IsNull(type.GetField("_backendPreview", flags).GetValue(editor));
                    Assert.IsNull(type.GetField("_backendPreviewSignature", flags).GetValue(editor));
                    Assert.IsNull(type.GetField("_backendInfo", flags).GetValue(editor));
                    Assert.IsNotNull(type.GetField("_previewFollow", flags).GetValue(editor), "Follow controls must work before Play.");
                }
            }
            finally
            {
                if (window != null) window.Close();
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
                SessionState.SetBool(expandedKey, wasExpanded);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StoppedInspectorDefersEditsUndoAndSaveUntilPlay(bool playThenStop)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            UnityEditor.Editor editor = null;
            try
            {
                var authoring = root.GetComponent<FluxParticleAuthoring>();
                var settings = new SerializedObject(authoring);
                settings.FindProperty("particleCount").intValue = 128;
                settings.ApplyModifiedPropertiesWithoutUndo();
                editor = UnityEditor.Editor.CreateEditor(authoring, typeof(FluxParticleAuthoringEditor));
                var type = typeof(FluxParticleAuthoringEditor);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var action = type.GetMethod("ApplyPreviewAction", flags);
                var previewField = type.GetField("_particlePreview", flags);
                if (playThenStop)
                {
                    action.Invoke(editor, new object[] { 0 });
                    Assert.IsNotNull(previewField.GetValue(editor));
                    action.Invoke(editor, new object[] { 2 });
                }
                var build = type.GetField("_backendPreview", flags).GetValue(editor);
                var signature = type.GetField("_backendPreviewSignature", flags).GetValue(editor);
                for (var count = 129; count <= 132; count++)
                {
                    settings.Update();
                    settings.FindProperty("particleCount").intValue = count;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    type.GetMethod("InvalidatePreviewSettings", flags).Invoke(editor, null);
                    type.GetMethod("OnPreviewSceneSaved", flags).Invoke(editor, new object[] { root.scene });
                    type.GetMethod("UpdateParticlePreview", flags).Invoke(editor, null);
                    type.GetMethod("EnsureParticlePreview", flags).Invoke(editor, null);
                    Assert.IsNull(previewField.GetValue(editor), "Stopped edits must not allocate GPU resources.");
                    Assert.AreSame(build, type.GetField("_backendPreview", flags).GetValue(editor));
                    Assert.AreSame(signature, type.GetField("_backendPreviewSignature", flags).GetValue(editor),
                        "Stopped edits must not serialize settings or build shaders.");
                }
                action.Invoke(editor, new object[] { 0 });
                var preview = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.IsNotNull(preview, (string)type.GetField("_backendPreviewError", flags).GetValue(editor));
                Assert.AreEqual(132, preview.Count, "Play must use the latest edited settings.");
                preview.Step(0.25f);
                Assert.AreEqual(0.25, preview.PlaybackTime, 0.0001);
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PausedInspectorDefersRebuildAndPreservesFollowUntilResume(bool editSettings)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            UnityEditor.Editor editor = null;
            try
            {
                var authoring = root.GetComponent<FluxParticleAuthoring>();
                var settings = new SerializedObject(authoring);
                settings.FindProperty("particleCount").intValue = 128;
                settings.ApplyModifiedPropertiesWithoutUndo();
                editor = UnityEditor.Editor.CreateEditor(authoring, typeof(FluxParticleAuthoringEditor));
                var type = typeof(FluxParticleAuthoringEditor);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var action = type.GetMethod("ApplyPreviewAction", flags);
                var previewField = type.GetField("_particlePreview", flags);
                action.Invoke(editor, new object[] { 0 });
                var first = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.IsNotNull(first);
                first.Step(0.25f);
                action.Invoke(editor, new object[] { 0 });
                if (editSettings)
                {
                    settings.Update();
                    settings.FindProperty("particleCount").intValue = 256;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                type.GetMethod("UpdateParticlePreview", flags).Invoke(editor, null);
                Assert.AreSame(first, previewField.GetValue(editor));
                Assert.AreEqual(0.25, first.PlaybackTime, 0.0001);
                action.Invoke(editor, new object[] { 0 });
                var resumed = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.IsNotNull(resumed);
                if (editSettings)
                {
                    Assert.AreNotSame(first, resumed);
                    Assert.AreEqual(256, resumed.Count);
                }
                else
                {
                    Assert.AreSame(first, resumed, "Unchanged settings must resume the paused simulation.");
                    Assert.AreEqual(0.25, resumed.PlaybackTime, 0.0001);
                }
                Assert.AreSame(first.Follow, resumed.Follow);
                action.Invoke(editor, new object[] { 0 });
                type.GetMethod("OnPreviewSceneSaved", flags).Invoke(editor, new object[] { root.scene });
                type.GetMethod("UpdateParticlePreview", flags).Invoke(editor, null);
                Assert.AreSame(resumed, previewField.GetValue(editor), "Saving while paused must defer GPU recreation.");
                action.Invoke(editor, new object[] { 0 });
                var afterSave = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.AreNotSame(resumed, afterSave);
                Assert.AreEqual(resumed.PlaybackTime, afterSave.PlaybackTime);
                Assert.AreSame(resumed.Follow, afterSave.Follow);
                action.Invoke(editor, new object[] { 1 });
                Assert.AreEqual(0, ((FluxParticleEditorPreview)previewField.GetValue(editor)).PlaybackTime);
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ManualBackendRefreshDoesNotCreateOrReplaceGpuPreview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            UnityEditor.Editor editor = null;
            try
            {
                var authoring = root.GetComponent<FluxParticleAuthoring>();
                var settings = new SerializedObject(authoring);
                settings.FindProperty("particleCount").intValue = 128;
                settings.ApplyModifiedPropertiesWithoutUndo();
                editor = UnityEditor.Editor.CreateEditor(authoring, typeof(FluxParticleAuthoringEditor));
                var type = typeof(FluxParticleAuthoringEditor);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var refresh = type.GetMethod("RefreshBackendInfo", flags);
                var previewField = type.GetField("_particlePreview", flags);
                refresh.Invoke(editor, null);
                Assert.IsNotNull(type.GetField("_backendInfo", flags).GetValue(editor));
                Assert.IsNull(type.GetField("_backendPreview", flags).GetValue(editor));
                Assert.IsNull(previewField.GetValue(editor));
                var action = type.GetMethod("ApplyPreviewAction", flags);
                action.Invoke(editor, new object[] { 0 });
                action.Invoke(editor, new object[] { 0 });
                var preview = previewField.GetValue(editor);
                var build = type.GetField("_backendPreview", flags).GetValue(editor);
                settings.Update();
                settings.FindProperty("particleCount").intValue = 256;
                settings.ApplyModifiedPropertiesWithoutUndo();
                refresh.Invoke(editor, null);
                Assert.AreSame(preview, previewField.GetValue(editor));
                Assert.AreSame(build, type.GetField("_backendPreview", flags).GetValue(editor));
                Assert.AreEqual(256, type.GetField("_backendInfoCount", flags).GetValue(editor));
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator PrefabAssetInspectorDoesNotCreatePreviewOrRestoreDeletedAsset()
        {
            return VerifyPrefabAssetSelectionAndDeletion(false);
        }

        [UnityTest]
        public IEnumerator PrefabVariantInspectorDoesNotCreatePreviewOrRestoreDeletedAsset()
        {
            return VerifyPrefabAssetSelectionAndDeletion(true);
        }

        [TestCase(4097)]
        [TestCase(16385)]
        public void InspectorPreviewUsesRuntimeCountAndSpawnsLastSlot(int count)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat) || !SystemInfo.supports32bitsIndexBuffer)
            {
                Assert.Ignore("GPU with RGBAFloat textures and 32bit mesh indices is required.");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            UnityEditor.Editor editor = null;
            try
            {
                var system = root.GetComponent<FluxParticleSystem>();
                var authoring = root.GetComponent<FluxParticleAuthoring>();
                if (authoring == null) authoring = root.AddComponent<FluxParticleAuthoring>();
                var systemObject = new SerializedObject(authoring);
                systemObject.FindProperty("particleCount").intValue = count;
                systemObject.ApplyModifiedPropertiesWithoutUndo();
                var authoringObject = new SerializedObject(authoring);
                authoringObject.FindProperty("emission.rate").floatValue = 0;
                authoringObject.FindProperty("emission.bursts").arraySize = 1;
                var burst = authoringObject.FindProperty("emission.bursts").GetArrayElementAtIndex(0);
                burst.FindPropertyRelative("time").floatValue = 0;
                burst.FindPropertyRelative("count").intValue = count;
                authoringObject.ApplyModifiedPropertiesWithoutUndo();
                editor = UnityEditor.Editor.CreateEditor(system.GetComponent<FluxParticleAuthoring>(), typeof(FluxParticleAuthoringEditor));
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(FluxParticleAuthoringEditor).GetMethod("ApplyPreviewAction", flags).Invoke(editor, new object[] { 0 });
                var preview = (FluxParticleEditorPreview)typeof(FluxParticleAuthoringEditor).GetField("_particlePreview", flags).GetValue(editor);
                Assert.IsNotNull(preview, (string)typeof(FluxParticleAuthoringEditor).GetField("_backendPreviewError", flags).GetValue(editor));
                Assert.AreEqual(authoring.ParticleCount, preview.Count);
                var mesh = (Mesh)typeof(FluxParticleEditorPreview).GetField("mesh", flags).GetValue(preview);
                Assert.AreEqual(count * 4, mesh.vertexCount);
                Assert.AreEqual(count * 4 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16, mesh.indexFormat);
                Assert.AreEqual(count - 1, mesh.uv2[mesh.vertexCount - 1].x);
                preview.Step(0.125f);
                var build = (FluxParticleBuildResult)typeof(FluxParticleAuthoringEditor).GetField("_backendPreview", flags).GetValue(editor);
                var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview).GetField("session", flags).GetValue(preview);
                Assert.AreEqual(0, ReadValue(session, build, "particle.age", count - 1).x, 0.0001f);
                Assert.AreEqual(count - 1, ReadValue(session, build, "particle.spawn-key", count - 1).x, 0.0001f);
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(FluxParticleRenderMode.Billboard, FluxParticleSimulationSpace.Local)]
        [TestCase(FluxParticleRenderMode.Billboard, FluxParticleSimulationSpace.World)]
        [TestCase(FluxParticleRenderMode.Mesh, FluxParticleSimulationSpace.Local)]
        [TestCase(FluxParticleRenderMode.Mesh, FluxParticleSimulationSpace.World)]
        public void PreviewAndRuntimeMeshesUseAuthoredCullingBounds(FluxParticleRenderMode mode, FluxParticleSimulationSpace space)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var root = new GameObject("Culling Bounds Test");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var runtimeRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var source = new Mesh();
            Mesh runtimeMesh = null;
            try
            {
                source.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
                source.triangles = new[] { 0, 1, 2 };
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var bounds = new Bounds(new Vector3(3, -2, 5), new Vector3(12, 8, 20));
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("simulationSpace").intValue = (int)space;
                serialized.FindProperty("render.mode").intValue = (int)mode;
                serialized.FindProperty("render.mesh").objectReferenceValue = source;
                serialized.FindProperty("render.cullingBounds").boundsValue = bounds;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var compiled = FluxParticleBuildPipeline.Build(authoring);
                using (var preview = new FluxParticleEditorPreview(compiled, authoring, 4))
                {
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var previewMesh = (Mesh)typeof(FluxParticleEditorPreview).GetField("mesh", flags).GetValue(preview);
                    Assert.AreEqual(bounds, previewMesh.bounds);
                    var renderer = runtimeRoot.GetComponentInChildren<FluxParticleRenderer>(true);
                    typeof(FluxParticleRenderer).GetField("cullingBounds", flags).SetValue(renderer, bounds);
                    var signature = mode == FluxParticleRenderMode.Mesh ? new[] { typeof(Mesh), typeof(int) } : new[] { typeof(int) };
                    var arguments = mode == FluxParticleRenderMode.Mesh ? new object[] { source, 4 } : new object[] { 4 };
                    runtimeMesh = (Mesh)typeof(FluxParticleRenderer).GetMethod("CreateParticleMesh", flags, null, signature, null)
                        .Invoke(renderer, arguments);
                    Assert.AreEqual(previewMesh.bounds, runtimeMesh.bounds);
                }
            }
            finally
            {
                if (runtimeMesh != null) Object.DestroyImmediate(runtimeMesh);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(runtimeRoot);
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(0f, false)]
        [TestCase(-1f, false)]
        [TestCase(float.NaN, false)]
        [TestCase(float.PositiveInfinity, false)]
        [TestCase(float.NaN, true)]
        [TestCase(float.NegativeInfinity, true)]
        public void InvalidCullingBoundsAreRejected(float value, bool changeCenter)
        {
            var root = new GameObject("Invalid Bounds Test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("render.cullingBounds").boundsValue = changeCenter
                    ? new Bounds(new Vector3(value, 0, 0), Vector3.one)
                    : new Bounds(Vector3.zero, new Vector3(1, value, 1));
                serialized.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.Contains("culling bounds", Assert.Throws<InvalidOperationException>(() =>
                    FluxParticleBuildPipeline.Build(authoring)).Message);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

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
                editor = UnityEditor.Editor.CreateEditor(system.GetComponent<FluxParticleAuthoring>(), typeof(FluxParticleAuthoringEditor));
                var type = typeof(FluxParticleAuthoringEditor);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var ensure = type.GetMethod("EnsureParticlePreview", flags);
                var previewField = type.GetField("_particlePreview", flags);
                type.GetMethod("ApplyPreviewAction", flags).Invoke(editor, new object[] { 0 });
                var first = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.IsNotNull(first, (string)type.GetField("_backendPreviewError", flags).GetValue(editor));
                first.Step(0.25f);
                ensure.Invoke(editor, null);
                Assert.AreSame(first, previewField.GetValue(editor));
                var signatureField = type.GetField("_backendPreviewSignature", flags);
                var signature = signatureField.GetValue(editor);
                ensure.Invoke(editor, null);
                Assert.AreSame(signature, signatureField.GetValue(editor), "Unchanged settings must reuse the cached signature.");
                var authoring = system.GetComponent<FluxParticleAuthoring>();
                EditorUtility.SetDirty(authoring);
                ensure.Invoke(editor, null);
                Assert.AreSame(first, previewField.GetValue(editor), "Dirty state alone must not reset simulation.");
                var settings = new SerializedObject(authoring);
                var originalRate = settings.FindProperty("emission.rate").floatValue;
                settings.FindProperty("emission.rate").floatValue = originalRate + 1;
                settings.ApplyModifiedProperties();
                ensure.Invoke(editor, null);
                var changed = previewField.GetValue(editor);
                Assert.IsNotNull(changed);
                Assert.AreNotSame(first, changed, "Changed settings must recreate preview.");
                type.GetMethod("InvalidatePreviewSettings", flags).Invoke(editor, null);
                ensure.Invoke(editor, null);
                Assert.AreSame(changed, previewField.GetValue(editor), "Invalidation without a value change must preserve preview.");
                type.GetField("_previewPlaying", flags).SetValue(editor, false);
                type.GetMethod("OnDisable", flags).Invoke(editor, null);
                type.GetMethod("OnEnable", flags).Invoke(editor, null);
                ensure.Invoke(editor, null);
                Assert.IsNull(previewField.GetValue(editor), "Reopening a stopped Inspector must not allocate GPU resources.");
                type.GetMethod("ApplyPreviewAction", flags).Invoke(editor, new object[] { 0 });
                var second = (FluxParticleEditorPreview)previewField.GetValue(editor);
                Assert.IsNotNull(second, (string)type.GetField("_backendPreviewError", flags).GetValue(editor));
                Assert.AreNotSame(first, second);
                Assert.IsTrue((bool)type.GetField("_previewPlaying", flags).GetValue(editor));
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
                var allocation = (FluxBuffer[])typeof(FluxParticleEditorPreview)
                    .GetField("allocationLevels", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                var allocationTextures = allocation.Select(buffer => buffer.Texture).ToArray();
                var allocationScene = allocation[0].gameObject.scene;
                Assert.IsTrue(EditorSceneManager.IsPreviewScene(allocationScene));
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
                Assert.IsTrue(allocationTextures.All(texture => texture == null));
                Assert.IsFalse(allocationScene.IsValid());
                Assert.Throws<ObjectDisposedException>(() => session.Step());
                Assert.DoesNotThrow(() => preview.Dispose());
            }
            finally
            {
                preview?.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PreviewReusesExpiredSlotsWhenCursorPointsToLivingParticle()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var root = new GameObject("Preview Free Slot Test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("lifetime.lifetime").floatValue = 0.5f;
                serialized.FindProperty("emission.rate").floatValue = 8;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var build = FluxParticleBuildPipeline.Build(authoring);
                using (var preview = new FluxParticleEditorPreview(build, authoring, 4))
                {
                    var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                        .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                    preview.Step(0.375f);
                    preview.Step(0.125f);
                    preview.Step(0.375f);
                    Assert.AreEqual(-1, ReadValue(session, build, "particle.age", 0).x, 0.0001f);
                    Assert.AreEqual(0.375f, ReadValue(session, build, "particle.age", 3).x, 0.0001f);
                    preview.Step(0.125f);
                    Assert.AreEqual(0, ReadValue(session, build, "particle.age", 0).x, 0.0001f);
                    Assert.AreEqual(7, ReadValue(session, build, "particle.spawn-key", 0).x, 0.0001f);
                    Assert.AreEqual(-1, ReadValue(session, build, "particle.age", 1).x, 0.0001f);
                    Assert.AreEqual(-1, ReadValue(session, build, "particle.age", 3).x, 0.0001f);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PreviewAtCapacityKeepsLivingParticleAndReusesSlotAfterExpiration()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var root = new GameObject("Preview Capacity Test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("lifetime.lifetime").floatValue = 0.5f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var build = FluxParticleBuildPipeline.Build(authoring);
                using (var preview = new FluxParticleEditorPreview(build, authoring, 1))
                {
                    var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                        .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                    preview.Step(0.25f);
                    var key = ReadValue(session, build, "particle.spawn-key").x;
                    var velocity = ReadValue(session, build, "particle.velocity");
                    preview.Step(0.25f);
                    Assert.AreEqual(key, ReadValue(session, build, "particle.spawn-key").x, 0.0001f);
                    Assert.AreEqual(0.25f, ReadValue(session, build, "particle.age").x, 0.0001f);
                    var position = ReadValue(session, build, "particle.position");
                    for (var component = 0; component < 3; component++)
                    {
                        Assert.AreEqual(velocity[component] * 0.25f, position[component], 0.0001f);
                    }
                    preview.Step(0.25f);
                    Assert.AreEqual(-1, ReadValue(session, build, "particle.age").x, 0.0001f);
                    preview.Step(0.25f);
                    Assert.AreEqual(0, ReadValue(session, build, "particle.age").x, 0.0001f);
                    Assert.AreNotEqual(key, ReadValue(session, build, "particle.spawn-key").x);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
        [Test]
        public void PreviewRecreationPreservesEmissionTimelineAndFraction()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            {
                Assert.Ignore("GPU with RGBAFloat textures is required.");
            }
            var root = new GameObject("Preview Emission Snapshot Test");
            try
            {
                var authoring = root.AddComponent<FluxParticleAuthoring>();
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("playback.startDelay").floatValue = 0.25f;
                serialized.FindProperty("playback.duration").floatValue = 0.5f;
                serialized.FindProperty("emission.rate").floatValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var build = FluxParticleBuildPipeline.Build(authoring);
                using (var first = new FluxParticleEditorPreview(build, authoring, 4))
                {
                    first.Step(0.375f);
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var type = typeof(FluxParticleEditorPreview);
                    var stateField = type.GetField("emissionState", flags);
                    using (var second = first.Recreate(build))
                    {
                        Assert.AreNotSame(stateField.GetValue(first), stateField.GetValue(second));
                        CollectionAssert.AreEqual((double[])stateField.GetValue(first), (double[])stateField.GetValue(second));
                        foreach (var delta in new[] { 0.125f, 0.25f, 0.125f, 0.125f })
                        {
                            first.Step(delta);
                            second.Step(delta);
                            Assert.AreEqual(first.PlaybackTime, second.PlaybackTime);
                            CollectionAssert.AreEqual((double[])stateField.GetValue(first), (double[])stateField.GetValue(second));
                            Assert.AreEqual(type.GetField("cursor", flags).GetValue(first), type.GetField("cursor", flags).GetValue(second));
                            Assert.AreEqual(type.GetField("seed", flags).GetValue(first), type.GetField("seed", flags).GetValue(second));
                        }
                        second.Restart();
                        Assert.AreEqual(0, second.PlaybackTime);
                        Assert.Greater(first.PlaybackTime, 0);
                    }
                }
            }
            finally
            {
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
        [TestCase(true)]
        [TestCase(false)]
        public void NoisePreviewReceivesSimulationTime(bool initiallyEnabled)
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
                typeof(TpLab.Flux.FX.Scripts.Modules.FluxParticleModule).GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(authoring.Modules[0], initiallyEnabled);
                var build = FluxParticleBuildPipeline.Build(authoring);
                preview = new FluxParticleEditorPreview(build, authoring, 4);
                var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview)
                    .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
                session.SetFloat("spawn.speed", 0);
                preview.Step(0.25f);
                preview.Step(0.25f);
                var velocity = ReadValue(session, build, "particle.velocity");
                Assert.AreEqual(initiallyEnabled ? Mathf.Sin(0.25f) * 0.25f : 0, velocity.x, 0.0005f);
                Assert.AreEqual(initiallyEnabled ? Mathf.Sin(0.25f * 1.17f) * 0.25f : 0, velocity.y, 0.0005f);
                Assert.AreEqual(initiallyEnabled ? Mathf.Sin(0.25f * 0.83f) * 0.25f : 0, velocity.z, 0.0005f);
            }
            finally
            {
                preview?.Dispose();
                Object.DestroyImmediate(root);
            }
        }
        static Vector4 ReadValue(FluxParticleProgramGpuSession session, FluxParticleBuildResult build, string id, int slot = 0)
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
                var pixel = (Vector4)texture.GetPixel(slot % page.width, slot / page.width);
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
        static IEnumerator VerifyPrefabAssetSelectionAndDeletion(bool variant)
        {
            var path = "Assets/FluxFX/Tests/PreviewAsset_" + Guid.NewGuid().ToString("N") + ".prefab";
            var previousSelection = Selection.objects;
            UnityEditor.Editor editor = null;
            try
            {
                Selection.objects = Array.Empty<Object>();
                yield return FluxParticleEditorTestWait.ForDelayCall();
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
                var contents = (GameObject)PrefabUtility.InstantiatePrefab(source);
                try
                {
                    if (!variant)
                    {
                        PrefabUtility.UnpackPrefabInstance(contents, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                    if (contents.GetComponent<FluxParticleAuthoring>() == null)
                    {
                        contents.AddComponent<FluxParticleAuthoring>();
                    }
                    // UdonSharpの初期化で登録された遅延処理を、対象の破棄前に完了させる。
                    yield return FluxParticleEditorTestWait.ForDelayCall();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    yield return FluxParticleEditorTestWait.ForDelayCall();
                }
                finally
                {
                    Object.DestroyImmediate(contents);
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.AreEqual(variant ? PrefabAssetType.Variant : PrefabAssetType.Regular, PrefabUtility.GetPrefabAssetType(prefab));
                var before = File.ReadAllBytes(path);
                var system = prefab.GetComponent<FluxParticleSystem>();
                Assert.IsNotNull(system.GetComponent<FluxParticleAuthoring>());
                editor = UnityEditor.Editor.CreateEditor(system.GetComponent<FluxParticleAuthoring>(), typeof(FluxParticleAuthoringEditor));
                var type = typeof(FluxParticleAuthoringEditor);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Assert.IsFalse((bool)type.GetProperty("HasScenePreview", flags).GetValue(editor));
                Assert.AreNotSame(editor, type.GetProperty("PreviewEditor", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
                type.GetMethod("EnsureParticlePreview", flags).Invoke(editor, null);
                type.GetMethod("UpdateParticlePreview", flags).Invoke(editor, null);
                yield return FluxParticleEditorTestWait.ForDelayCall();
                Assert.IsNull(type.GetField("_particlePreview", flags).GetValue(editor));
                Assert.IsNull(type.GetField("_backendPreview", flags).GetValue(editor));
                CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
                Assert.IsTrue(AssetDatabase.DeleteAsset(path));
                type.GetMethod("UpdateParticlePreview", flags).Invoke(editor, null);
                type.GetMethod("EnsureParticlePreview", flags).Invoke(editor, null);
                yield return FluxParticleEditorTestWait.ForDelayCall();
                Assert.IsFalse(File.Exists(path));
                Assert.IsFalse(File.Exists(path + ".meta"));
                Assert.IsNull(type.GetField("_particlePreview", flags).GetValue(editor));
            }
            finally
            {
                if (editor != null)
                {
                    Object.DestroyImmediate(editor);
                }
                Selection.objects = previousSelection;
                AssetDatabase.DeleteAsset(path);
            }
        }

    }
}
