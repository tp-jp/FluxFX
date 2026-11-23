using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Authoring;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// System追従の設定保持、座標変換、GPU生成と非破壊Previewを検証します。
    /// </summary>
    public sealed class FluxParticleSystemFollowTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        Scene _scene;
        GameObject _root;
        Transform _target;
        FluxParticleAuthoring _authoring;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _root = new GameObject("Follow tests");
            SceneManager.MoveGameObjectToScene(_root, _scene);
            _authoring = _root.AddComponent<FluxParticleAuthoring>();
            _target = new GameObject("Follow target").transform;
            SceneManager.MoveGameObjectToScene(_target.gameObject, _scene);
            _target.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(25, 60, 15));
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("initialVelocity.speed").floatValue = 0;
            settings.FindProperty("lifetime.lifetime").floatValue = 10;
            settings.FindProperty("emission.rate").floatValue = 10;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return FluxParticleEditorTestWait.ForDelayCall();
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void DefaultAndOldSerializedSettingsRemainOffWithoutChangingShaderOrStateLayout()
        {
            Assert.AreEqual(FluxParticleFollowTarget.Off, _authoring.SystemFollow.Target);
            var baseline = FluxParticleBuildPipeline.Build(_authoring);
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            var enabled = FluxParticleBuildPipeline.Build(_authoring);
            CollectionAssert.AreEqual(baseline.Shaders.Passes.Select(value => value.Source), enabled.Shaders.Passes.Select(value => value.Source));
            Assert.AreEqual(baseline.RendererSource, enabled.RendererSource);
            CollectionAssert.AreEquivalent(baseline.InitialValues, enabled.InitialValues);
            var legacy = new GameObject("Legacy settings");
            SceneManager.MoveGameObjectToScene(legacy, _scene);
            var authoring = legacy.AddComponent<FluxParticleAuthoring>();
            JsonUtility.FromJsonOverwrite("{\"particleCount\":123,\"playOnAwake\":false}", authoring);
            Assert.AreEqual(123, authoring.ParticleCount);
            Assert.IsFalse(authoring.PlayOnAwake);
            Assert.AreEqual(FluxParticleFollowTarget.Off, authoring.SystemFollow.Target);
        }

        [TestCase(FluxParticleFollowOffsetSpace.World)]
        [TestCase(FluxParticleFollowOffsetSpace.TargetRotation)]
        public void OffsetIgnoresTargetScale(FluxParticleFollowOffsetSpace space)
        {
            _target.localScale = new Vector3(3, 5, 8);
            var offset = new Vector3(1, 2, -3);
            var expected = _target.position + (space == FluxParticleFollowOffsetSpace.World ? offset : _target.rotation * offset);
            AssertVector(expected, FluxParticleFollow.Position(_target.position, _target.rotation, offset, space));
        }

        [TestCase(FluxParticleFollowRotation.None)]
        [TestCase(FluxParticleFollowRotation.HorizontalOnly)]
        [TestCase(FluxParticleFollowRotation.FullRotation)]
        public void RotationPolicyHasNoScaleOrUnwantedTilt(FluxParticleFollowRotation mode)
        {
            var current = Quaternion.Euler(10, 120, 20);
            var result = FluxParticleFollow.Rotation(_target.rotation, current, mode);
            if (mode == FluxParticleFollowRotation.None) Assert.Less(Quaternion.Angle(current, result), 0.001f);
            else if (mode == FluxParticleFollowRotation.FullRotation) Assert.Less(Quaternion.Angle(_target.rotation, result), 0.001f);
            else
            {
                AssertVector(Vector3.up, result * Vector3.up);
                var expected = _target.forward;
                expected.y = 0;
                AssertVector(expected.normalized, result * Vector3.forward);
            }
        }

        [Test]
        public void LookingStraightUpPreservesPreviousHeading()
        {
            var result = FluxParticleFollow.Rotation(Quaternion.Euler(-90, 40, 0), Quaternion.Euler(0, 115, 0),
                FluxParticleFollowRotation.HorizontalOnly);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0, 115, 0), result), 0.001f);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void InvalidTransformTargetsAreRejected(int target)
        {
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            var settings = new SerializedObject(_authoring);
            if (target == 2) _target.SetParent(_root.transform);
            settings.FindProperty("systemFollow.targetTransform").objectReferenceValue = target == 0 ? null :
                target == 1 ? _root.transform : _target;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleAuthoringValidator.Validate(_authoring));
        }

        [Test]
        public void NonFiniteOffsetIsRejectedOnlyWhileFollowIsEnabled()
        {
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("systemFollow.positionOffset").vector3Value = new Vector3(float.NaN, 0, 0);
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => FluxParticleAuthoringValidator.Validate(_authoring));
            settings.FindProperty("systemFollow.target").intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.DoesNotThrow(() => FluxParticleAuthoringValidator.Validate(_authoring));
        }

        [TestCase(FluxParticleFollowTarget.LocalPlayerPosition)]
        [TestCase(FluxParticleFollowTarget.LocalPlayerHead)]
        [TestCase(FluxParticleFollowTarget.SpecifiedTransform)]
        public void PreviewMatchesMovingRealTransformWithScaledParentWithoutChangingAuthoring(FluxParticleFollowTarget target)
        {
            SetFollow(target);
            var parent = new GameObject("Scaled parent").transform;
            SceneManager.MoveGameObjectToScene(parent.gameObject, _scene);
            parent.SetPositionAndRotation(new Vector3(1, -3, 2), Quaternion.Euler(13, 25, 17));
            parent.localScale = new Vector3(2, 3, 4);
            _root.transform.SetParent(parent, false);
            _root.transform.localScale = new Vector3(0.5f, 0.75f, 1.25f);
            var expected = new GameObject("Physical follow").transform;
            expected.SetParent(parent, false);
            expected.localScale = _root.transform.localScale;
            var before = EditorJsonUtility.ToJson(_root.transform);
            var settingsBefore = EditorJsonUtility.ToJson(_authoring);
            var dirty = _scene.isDirty;
            using (var preview = new FluxParticleEditorPreview(FluxParticleBuildPipeline.Build(_authoring), _authoring, 4))
            {
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform, _target);
                for (var i = 0; i < 3; i++)
                {
                    _target.position += new Vector3(1, 0.5f, -2);
                    _target.Rotate(10, 15, 5);
                    expected.SetPositionAndRotation(_target.position + Vector3.up * 2, _target.rotation);
                    var matrix = preview.SystemMatrix;
                    for (var index = 0; index < 16; index++) Assert.AreEqual(expected.localToWorldMatrix[index], matrix[index], 0.0001f);
                    var reference = new Vector3(1, 2, 3);
                    AssertVector(reference, preview.WorldToReference(preview.ReferenceToWorld(reference)));
                }
                using (var recreated = preview.Recreate(FluxParticleBuildPipeline.Build(_authoring)))
                {
                    AssertVector(preview.ReferenceToWorld(Vector3.one), recreated.ReferenceToWorld(Vector3.one));
                }
            }
            Assert.AreEqual(before, EditorJsonUtility.ToJson(_root.transform));
            Assert.AreEqual(settingsBefore, EditorJsonUtility.ToJson(_authoring));
            Assert.AreEqual(dirty, _scene.isDirty);
        }

        [TestCase(FluxParticleSimulationSpace.World)]
        [TestCase(FluxParticleSimulationSpace.Local)]
        public void GpuEmissionFollowsWhileExistingParticlesRespectSimulationSpace(FluxParticleSimulationSpace space)
        {
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("simulationSpace").intValue = (int)space;
            settings.FindProperty("systemFollow.rotation").intValue = (int)FluxParticleFollowRotation.None;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var build = FluxParticleBuildPipeline.Build(_authoring);
            using (var preview = new FluxParticleEditorPreview(build, _authoring, 4))
            {
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform, _target);
                preview.Step(0.1f);
                var firstOrigin = _target.position + Vector3.up * 2;
                AssertVector(firstOrigin, preview.ReferenceToWorld(Read(preview, build, "particle.position", 0)));
                _target.position += Vector3.right * 7;
                preview.Step(0.1f);
                var nextOrigin = _target.position + Vector3.up * 2;
                AssertVector(space == FluxParticleSimulationSpace.World ? firstOrigin : nextOrigin,
                    preview.ReferenceToWorld(Read(preview, build, "particle.position", 0)));
                var newSlot = Enumerable.Range(0, 4).Single(slot => Read(preview, build, "particle.spawn-key", slot).x == 1 &&
                    Read(preview, build, "particle.age", slot).x == 0);
                AssertVector(nextOrigin, preview.ReferenceToWorld(Read(preview, build, "particle.position", newSlot)));
            }
        }

        [Test]
        public void FirstBurstUsesFollowedPositionAndMissingTargetWaits()
        {
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("simulationSpace").intValue = (int)FluxParticleSimulationSpace.World;
            settings.FindProperty("emission.rate").floatValue = 0;
            settings.FindProperty("emission.bursts").arraySize = 1;
            settings.FindProperty("emission.bursts").GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var build = FluxParticleBuildPipeline.Build(_authoring);
            using (var preview = new FluxParticleEditorPreview(build, _authoring, 4))
            {
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform);
                preview.Step(0.1f);
                Assert.AreEqual(0, preview.PlaybackTime);
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform, _target);
                preview.Step(0.1f);
                AssertVector(_target.position + Vector3.up * 2, Read(preview, build, "particle.position", 0));
            }
        }

        [Test]
        public void BakePreservesTargetReferenceAndDoesNotCopyPreviewTarget()
        {
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
            var system = instance.GetComponent<FluxParticleSystem>();
            var executor = instance.GetComponentInChildren<FluxParticleExecutor>(true);
            var build = FluxParticleBuildPipeline.Build(_authoring);
            var type = typeof(FluxParticleProgramBuild).Assembly.GetType("TpLab.Flux.FX.Editor.Build.FluxParticleRuntimeApiBuilder", true);
            type.GetMethod("Configure").Invoke(null, new object[] { executor, _authoring, build.Shaders.Compilation.Parameters.Values.ToArray() });
            Assert.AreEqual(FluxParticleFollowTarget.SpecifiedTransform, system.GetProgramVariable("followTarget"));
            Assert.AreSame(_target, system.GetProgramVariable("followTransform"));
            Assert.AreEqual(Vector3.up * 2, system.GetProgramVariable("followPositionOffset"));
            Assert.AreEqual(FluxParticleFollowRotation.FullRotation, system.GetProgramVariable("followRotation"));
            Assert.IsNull(typeof(FluxParticleSystem).GetField("previewTarget", PrivateInstance));
            var runtimeBefore = EditorJsonUtility.ToJson(system);
            var authoringBefore = EditorJsonUtility.ToJson(_authoring);
            using (var preview = new FluxParticleEditorPreview(build, _authoring, 4))
            {
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform, instance.transform);
                preview.Follow.SetFrozen(true);
                preview.Step(0.1f);
                preview.Follow.SetFrozen(false);
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SceneCamera);
                preview.Follow.Configure(FluxParticlePreviewFollowMode.Placement);
                type.GetMethod("Configure").Invoke(null, new object[] { executor, _authoring, build.Shaders.Compilation.Parameters.Values.ToArray() });
            }
            Assert.AreEqual(runtimeBefore, EditorJsonUtility.ToJson(system));
            Assert.AreEqual(authoringBefore, EditorJsonUtility.ToJson(_authoring));
        }

        [TestCase(FluxParticleFollowTarget.LocalPlayerPosition)]
        [TestCase(FluxParticleFollowTarget.LocalPlayerHead)]
        [TestCase(FluxParticleFollowTarget.SpecifiedTransform)]
        public void PreviewDefaultsToPlacementIgnoringSavedTargetsAndOffset(FluxParticleFollowTarget target)
        {
            SetFollow(target);
            _root.transform.SetPositionAndRotation(new Vector3(9, 7, 5), Quaternion.Euler(10, 20, 30));
            var before = EditorJsonUtility.ToJson(_authoring);
            using (var preview = new FluxParticleEditorPreview(FluxParticleBuildPipeline.Build(_authoring), _authoring, 4))
            {
                Assert.AreEqual(FluxParticlePreviewFollowMode.Placement, preview.Follow.Mode);
                AssertMatrix(_root.transform.localToWorldMatrix, preview.SystemMatrix);
                _target.position += Vector3.one * 10;
                AssertMatrix(_root.transform.localToWorldMatrix, preview.SystemMatrix);
                Assert.IsFalse(preview.Follow.SetFrozen(true));
                preview.Step(0.1f);
                Assert.Greater(preview.PlaybackTime, 0);
            }
            Assert.AreEqual(before, EditorJsonUtility.ToJson(_authoring));
        }

        [TestCase(FluxParticleSimulationSpace.Local)]
        [TestCase(FluxParticleSimulationSpace.World)]
        public void FreezeKeepsPoseAndSimulationRunningAcrossRebuildWithoutWritingScene(FluxParticleSimulationSpace space)
        {
            SetFollow(FluxParticleFollowTarget.LocalPlayerHead);
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("simulationSpace").intValue = (int)space;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var before = EditorJsonUtility.ToJson(_authoring);
            var transformBefore = EditorJsonUtility.ToJson(_root.transform);
            var dirty = _scene.isDirty;
            var build = FluxParticleBuildPipeline.Build(_authoring);
            using (var preview = new FluxParticleEditorPreview(build, _authoring, 4))
            {
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform, _target);
                Assert.IsTrue(preview.Follow.SetFrozen(true));
                var frozen = preview.SystemMatrix;
                _target.SetPositionAndRotation(new Vector3(18, -4, 2), Quaternion.Euler(-15, 120, 45));
                preview.Step(0.1f);
                Assert.Greater(preview.PlaybackTime, 0);
                AssertMatrix(frozen, preview.SystemMatrix);
                AssertVector(frozen.MultiplyPoint3x4(Vector3.zero), preview.ReferenceToWorld(Read(preview, build, "particle.position", 0)));
                using (var rebuilt = preview.Recreate(build))
                {
                    Assert.IsTrue(rebuilt.Follow.IsFrozen);
                    Assert.AreSame(_target, rebuilt.Follow.Target);
                    AssertMatrix(frozen, rebuilt.SystemMatrix);
                    rebuilt.Step(0.1f);
                    Assert.Greater(rebuilt.PlaybackTime, preview.PlaybackTime);
                    Assert.IsTrue(rebuilt.Follow.SetFrozen(false));
                    AssertVector(_target.position + Vector3.up * 2, rebuilt.SystemMatrix.MultiplyPoint3x4(Vector3.zero));
                    Assert.Less(Quaternion.Angle(_target.rotation, rebuilt.SystemMatrix.rotation), 0.001f);
                    rebuilt.Follow.Configure(FluxParticlePreviewFollowMode.Placement);
                    Assert.IsFalse(rebuilt.Follow.IsFrozen);
                    AssertMatrix(_root.transform.localToWorldMatrix, rebuilt.SystemMatrix);
                }
            }
            Assert.AreEqual(before, EditorJsonUtility.ToJson(_authoring));
            Assert.AreEqual(transformBefore, EditorJsonUtility.ToJson(_root.transform));
            Assert.AreEqual(dirty, _scene.isDirty);
        }

        [Test]
        public void MissingPreviewTargetDoesNotFallBackToRuntimeTargetAndFrozenPoseSurvivesDeletion()
        {
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            using (var preview = new FluxParticleEditorPreview(FluxParticleBuildPipeline.Build(_authoring), _authoring, 4))
            {
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform);
                Assert.IsFalse(preview.Follow.HasTarget);
                Assert.IsFalse(preview.Follow.SetFrozen(true));
                preview.Step(0.1f);
                Assert.AreEqual(0, preview.PlaybackTime);
                preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform, _target);
                Assert.IsTrue(preview.Follow.SetFrozen(true));
                var frozen = preview.SystemMatrix;
                Object.DestroyImmediate(_target.gameObject);
                preview.Step(0.1f);
                Assert.Greater(preview.PlaybackTime, 0);
                AssertMatrix(frozen, preview.SystemMatrix);
                Assert.IsTrue(preview.Follow.SetFrozen(false));
                AssertMatrix(frozen, preview.SystemMatrix);
            }
        }

        [UnityTest]
        public IEnumerator SceneCameraFollowCanBeFrozenAndReleasedWithoutMovingAuthoring()
        {
            SetFollow(FluxParticleFollowTarget.LocalPlayerPosition);
            var previousView = SceneView.lastActiveSceneView;
            var view = ScriptableObject.CreateInstance<SceneView>();
            var before = EditorJsonUtility.ToJson(_authoring);
            var transformBefore = EditorJsonUtility.ToJson(_root.transform);
            var dirty = _scene.isDirty;
            try
            {
                view.Show();
                view.Focus();
                yield return null;
                Assert.AreSame(view, SceneView.lastActiveSceneView);
                using (var preview = new FluxParticleEditorPreview(FluxParticleBuildPipeline.Build(_authoring), _authoring, 4))
                {
                    preview.Follow.Configure(FluxParticlePreviewFollowMode.SceneCamera);
                    AssertVector(view.camera.transform.position + Vector3.up * 2, preview.SystemMatrix.MultiplyPoint3x4(Vector3.zero));
                    Assert.IsTrue(preview.Follow.SetFrozen(true));
                    var frozen = preview.SystemMatrix;
                    view.camera.transform.SetPositionAndRotation(new Vector3(19, 11, -25), Quaternion.Euler(20, 30, 0));
                    AssertMatrix(frozen, preview.SystemMatrix);
                    preview.Step(0.1f);
                    Assert.Greater(preview.PlaybackTime, 0);
                    preview.Follow.SetFrozen(false);
                    AssertVector(view.camera.transform.position + Vector3.up * 2, preview.SystemMatrix.MultiplyPoint3x4(Vector3.zero));
                }
            }
            finally
            {
                view.Close();
                if (previousView != null) previousView.Focus();
            }
            Assert.AreEqual(before, EditorJsonUtility.ToJson(_authoring));
            Assert.AreEqual(transformBefore, EditorJsonUtility.ToJson(_root.transform));
            Assert.AreEqual(dirty, _scene.isDirty);
        }

        void SetFollow(FluxParticleFollowTarget target)
        {
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("systemFollow.target").intValue = (int)target;
            settings.FindProperty("systemFollow.targetTransform").objectReferenceValue = _target;
            settings.FindProperty("systemFollow.previewTarget").objectReferenceValue = _target;
            settings.FindProperty("systemFollow.positionOffset").vector3Value = Vector3.up * 2;
            settings.FindProperty("systemFollow.rotation").intValue = (int)FluxParticleFollowRotation.FullRotation;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        [TestCase(FluxParticleSimulationSpace.World, 1)]
        [TestCase(FluxParticleSimulationSpace.Local, 1)]
        [TestCase(FluxParticleSimulationSpace.World, 4)]
        [TestCase(FluxParticleSimulationSpace.Local, 4)]
        public void FollowPreviewMatchesManualTransformRenderingIncludingTrailsAndDistanceFade(FluxParticleSimulationSpace space, int samples)
        {
            SetFollow(FluxParticleFollowTarget.SpecifiedTransform);
            typeof(FluxParticleAuthoring).GetField("modules", PrivateInstance).SetValue(_authoring,
                new List<FluxParticleModule> { new FluxParticleDistanceFadeModule(), new FluxParticleTrailsModule() });
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("simulationSpace").intValue = (int)space;
            settings.FindProperty("render.startSize").floatValue = 4;
            settings.FindProperty("render.intensity").floatValue = 4;
            settings.FindProperty("render.blendMode").intValue = (int)FluxParticleBlendMode.Alpha;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var automatic = CaptureFollowImage(true, space, samples);
            var manual = CaptureFollowImage(false, space, samples);
            var maximum = 0f;
            var peak = 0f;
            for (var i = 0; i < automatic.Length; i++)
            {
                peak = Mathf.Max(peak, automatic[i].maxColorComponent);
                for (var channel = 0; channel < 4; channel++)
                {
                    maximum = Mathf.Max(maximum, Mathf.Abs(automatic[i][channel] - manual[i][channel]));
                }
            }
            Assert.Greater(peak, 1, "Visible HDR particles are required; black images do not demonstrate parity.");
            Assert.Less(maximum, 0.0001f);
            File.WriteAllText("Temp/SystemFollow/preview-" + space + "-msaa" + samples + "-comparison.txt",
                "Maximum channel difference=" + maximum.ToString("R") + "; HDR peak=" + peak.ToString("R"));
        }

        Color[] CaptureFollowImage(bool automatic, FluxParticleSimulationSpace space, int samples)
        {
            var settings = new SerializedObject(_authoring);
            settings.FindProperty("systemFollow.target").intValue = automatic ? (int)FluxParticleFollowTarget.SpecifiedTransform : 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            _root.transform.SetPositionAndRotation(new Vector3(9, 0, 0), Quaternion.identity);
            var cameraObject = new GameObject("Follow comparison camera");
            SceneManager.MoveGameObjectToScene(cameraObject, _scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(0, 2, -6);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 31;
            camera.allowHDR = true;
            camera.allowMSAA = samples > 1;
            var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGBFloat) { antiAliasing = samples };
            var image = new Texture2D(256, 256, TextureFormat.RGBAFloat, false, true);
            var active = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                var build = FluxParticleBuildPipeline.Build(_authoring);
                using (var preview = new FluxParticleEditorPreview(build, _authoring, 32))
                {
                    if (automatic) preview.Follow.Configure(FluxParticlePreviewFollowMode.SpecifiedTransform, _target);
                    for (var frame = 0; frame < 8; frame++)
                    {
                        _target.SetPositionAndRotation(new Vector3(-0.7f + frame * 0.2f, 0, 0), Quaternion.identity);
                        if (!automatic) _root.transform.SetPositionAndRotation(_target.position + Vector3.up * 2, _target.rotation);
                        preview.Step(0.1f);
                    }
                    preview.Draw(camera, _root.transform.localToWorldMatrix, 31);
                    camera.Render();
                }
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Temp/SystemFollow");
                File.WriteAllBytes("Temp/SystemFollow/preview-" + space + "-msaa" + samples + (automatic ? "-follow.png" : "-manual.png"), image.EncodeToPNG());
                return image.GetPixels();
            }
            finally
            {
                RenderTexture.active = active;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
            }
        }

        static void AssertMatrix(Matrix4x4 expected, Matrix4x4 actual)
        {
            for (var index = 0; index < 16; index++) Assert.AreEqual(expected[index], actual[index], 0.0001f);
        }

        static void AssertVector(Vector3 expected, Vector3 actual) => Assert.Less(Vector3.Distance(expected, actual), 0.0002f,
            "Expected " + expected + ", actual " + actual);

        static Vector3 Read(FluxParticleEditorPreview preview, FluxParticleBuildResult build, string id, int slot)
        {
            var session = (FluxParticleProgramGpuSession)typeof(FluxParticleEditorPreview).GetField("session", PrivateInstance).GetValue(preview);
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
                var value = Vector3.zero;
                for (var i = 0; i < Math.Min(3, binding.ComponentCount); i++) value[i] = pixel[binding.ChannelOffset + i];
                return value;
            }
            finally
            {
                RenderTexture.active = active;
                Object.DestroyImmediate(texture);
            }
        }
    }
}
