using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TpLab.Flux.FX.Editor.Authoring;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    public sealed class FluxParticleSettingsMigrationTests
    {
        const string PrefabPath = "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab";
        readonly List<GameObject> _loaded = new List<GameObject>();
        Scene _scene;
        string _folder;
        GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _folder = "Assets/FluxFX/Tests/SettingsMigration_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/FluxFX/Tests", Path.GetFileName(_folder));
            _root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), _scene);
            PrefabUtility.UnpackPrefabInstance(_root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return FluxParticleEditorTestWait.ForDelayCall();
            foreach (var contents in _loaded)
            {
                if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
            }
            _loaded.Clear();
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            yield return FluxParticleEditorTestWait.ForDelayCall();
            AssetDatabase.DeleteAsset(_folder);
        }

        [Test]
        public void LegacySceneSettingsMigrateAndUndoWithoutChangingOtherSettings()
        {
            SetLegacy(_root, 321, false);
            var authoring = _root.GetComponent<FluxParticleAuthoring>();
            var settings = new SerializedObject(authoring);
            settings.FindProperty("shape.radius").floatValue = 3.75f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(FluxParticleSettingsMigration.NeedsMigration(authoring));
            FluxParticleSettingsMigration.Read(authoring, out var count, out var play);
            Assert.AreEqual(321, count);
            Assert.IsFalse(play);
            Assert.AreEqual(0, settings.FindProperty("settingsVersion").intValue, "Reading legacy data must not migrate it.");
            Undo.IncrementCurrentGroup();
            FluxParticleSettingsMigration.MigrateTarget(authoring);
            Assert.AreEqual(321, authoring.ParticleCount);
            Assert.IsFalse(authoring.PlayOnAwake);
            Assert.AreEqual(3.75f, authoring.Shape.Radius);
            Assert.IsFalse(FluxParticleSettingsMigration.NeedsMigration(authoring));
            Undo.PerformUndo();
            Assert.IsTrue(FluxParticleSettingsMigration.NeedsMigration(authoring));
            Undo.PerformRedo();
            Assert.AreEqual(321, authoring.ParticleCount);
            Assert.IsFalse(authoring.PlayOnAwake);
            Undo.ClearUndo(authoring);
            Undo.ClearUndo(_root.GetComponent<FluxParticleSystem>());
        }

        [Test]
        public void NativeAuthoringCopyIncludesMainSettingsAndPolymorphicModules()
        {
            var source = _root.GetComponent<FluxParticleAuthoring>();
            var settings = new SerializedObject(source);
            settings.FindProperty("particleCount").intValue = 777;
            settings.FindProperty("playOnAwake").boolValue = false;
            settings.FindProperty("shape.radius").floatValue = 7;
            var modules = settings.FindProperty("modules");
            modules.arraySize = 3;
            modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleGravityModule();
            modules.GetArrayElementAtIndex(1).managedReferenceValue = new FluxParticleSizeOverLifetimeModule();
            modules.GetArrayElementAtIndex(2).managedReferenceValue = new FluxParticleColorOverLifetimeModule();
            settings.ApplyModifiedPropertiesWithoutUndo();
            settings.Update();
            settings.FindProperty("modules").GetArrayElementAtIndex(1).FindPropertyRelative("curve").animationCurveValue =
                AnimationCurve.Linear(0, 2, 1, 6);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.blue, 1) },
                new[] { new GradientAlphaKey(0.25f, 0), new GradientAlphaKey(1, 1) });
            settings.FindProperty("modules").GetArrayElementAtIndex(2).FindPropertyRelative("gradient").gradientValue = gradient;
            var texture = new Texture2D(2, 2);
            AssetDatabase.CreateAsset(texture, _folder + "/Texture.asset");
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out var textureGuid, out long textureLocalId));
            settings.FindProperty("render.texture").objectReferenceValue = texture;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var other = new GameObject("Copy destination");
            SceneManager.MoveGameObjectToScene(other, _scene);
            var destination = other.AddComponent<FluxParticleAuthoring>();
            Assert.IsTrue(ComponentUtility.CopyComponent(source));
            Assert.IsTrue(ComponentUtility.PasteComponentValues(destination));
            Assert.AreEqual(777, destination.ParticleCount);
            Assert.IsFalse(destination.PlayOnAwake);
            Assert.AreEqual(7, destination.Shape.Radius);
            Assert.IsInstanceOf<FluxParticleGravityModule>(destination.Modules[0]);
            Assert.AreNotSame(source.Modules[0], destination.Modules[0]);
            Assert.AreEqual(4, ((FluxParticleSizeOverLifetimeModule)destination.Modules[1]).Curve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(Color.red, ((FluxParticleColorOverLifetimeModule)destination.Modules[2]).Gradient.colorKeys[0].color);
            Assert.AreEqual(0.25f, ((FluxParticleColorOverLifetimeModule)destination.Modules[2]).Gradient.alphaKeys[0].alpha, 0.001f);
            Assert.IsNotNull(destination.Render.Texture);
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(destination.Render.Texture, out var copiedGuid, out long copiedLocalId));
            Assert.AreEqual(textureGuid, copiedGuid, "Copy must preserve the texture asset reference.");
            Assert.AreEqual(textureLocalId, copiedLocalId, "Copy must preserve the referenced object within the asset.");
            var editor = UnityEditor.Editor.CreateEditor(source);
            try
            {
                Assert.IsInstanceOf<FluxParticleAuthoringEditor>(editor, "Settings owns the Inspector.");
            }
            finally
            {
                Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator VariantMigrationPreservesOverridesAndFutureParentChanges()
        {
            SetLegacy(_root, 100, true);
            yield return FluxParticleEditorTestWait.ForDelayCall();
            var basePath = _folder + "/Base.prefab";
            var variantPath = _folder + "/Variant.prefab";
            var baseAsset = PrefabUtility.SaveAsPrefabAsset(_root, basePath);
            var variantObject = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset, _scene);
            SetSystem(variantObject, 250, true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(variantObject.GetComponent<FluxParticleSystem>());
            yield return FluxParticleEditorTestWait.ForDelayCall();
            var variantAsset = PrefabUtility.SaveAsPrefabAsset(variantObject, variantPath);
            FluxParticleSettingsMigration.MigrateTarget(variantAsset.GetComponent<FluxParticleAuthoring>());
            yield return FluxParticleEditorTestWait.ForDelayCall();
            var child = Load(variantPath).GetComponent<FluxParticleAuthoring>();
            Assert.AreEqual(250, child.ParticleCount);
            Assert.IsTrue(child.PlayOnAwake);
            Assert.IsFalse(FluxParticleSettingsMigration.NeedsMigration(child));
            using (var properties = new SerializedObject(child))
            {
                Assert.IsTrue(properties.FindProperty("particleCount").prefabOverride);
                Assert.IsFalse(properties.FindProperty("playOnAwake").prefabOverride);
            }
            var parent = Load(basePath);
            var settings = new SerializedObject(parent.GetComponent<FluxParticleAuthoring>());
            settings.FindProperty("particleCount").intValue = 500;
            settings.FindProperty("playOnAwake").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(parent, basePath);
            var reloaded = Load(variantPath).GetComponent<FluxParticleAuthoring>();
            Assert.AreEqual(250, reloaded.ParticleCount);
            Assert.IsFalse(reloaded.PlayOnAwake, "Non-overridden settings must continue inheriting from the base.");
            FluxParticleSettingsMigration.MigrateTarget(AssetDatabase.LoadAssetAtPath<GameObject>(variantPath).GetComponent<FluxParticleAuthoring>());
            Assert.AreEqual(250, Load(variantPath).GetComponent<FluxParticleAuthoring>().ParticleCount);
            PrefabUtility.RevertPropertyOverride(new SerializedObject(reloaded).FindProperty("particleCount"), InteractionMode.AutomatedAction);
            Assert.AreEqual(500, reloaded.ParticleCount);
        }

        [UnityTest]
        public IEnumerator LegacySceneOverrideSurvivesMigratingBaseFirst()
        {
            SetLegacy(_root, 100, true);
            yield return FluxParticleEditorTestWait.ForDelayCall();
            var path = _folder + "/Base.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(_root, path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
            SetSystem(instance, 333, false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<FluxParticleSystem>());
            FluxParticleSettingsMigration.MigrateTarget(prefab.GetComponent<FluxParticleAuthoring>());
            var authoring = instance.GetComponent<FluxParticleAuthoring>();
            Assert.IsTrue(FluxParticleSettingsMigration.NeedsMigration(authoring));
            FluxParticleSettingsMigration.Read(authoring, out var count, out var play);
            Assert.AreEqual(333, count);
            Assert.IsFalse(play);
            FluxParticleSettingsMigration.MigrateTarget(authoring);
            Assert.AreEqual(333, authoring.ParticleCount);
            Assert.IsFalse(authoring.PlayOnAwake);
            Assert.IsFalse(FluxParticleSettingsMigration.NeedsMigration(authoring));
            yield return FluxParticleEditorTestWait.ForDelayCall();
        }

        [UnityTest]
        public IEnumerator NestedPrefabKeepsInheritedCountAndOverridesPlayback()
        {
            SetLegacy(_root, 100, true);
            yield return FluxParticleEditorTestWait.ForDelayCall();
            var basePath = _folder + "/Base.prefab";
            var outerPath = _folder + "/Outer.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(_root, basePath);
            var holder = new GameObject("Outer");
            SceneManager.MoveGameObjectToScene(holder, _scene);
            var inner = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
            inner.transform.SetParent(holder.transform, false);
            SetSystem(inner, 100, false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(inner.GetComponent<FluxParticleSystem>());
            yield return FluxParticleEditorTestWait.ForDelayCall();
            var outer = PrefabUtility.SaveAsPrefabAsset(holder, outerPath);
            FluxParticleSettingsMigration.MigrateTarget(outer.GetComponentInChildren<FluxParticleAuthoring>());
            yield return FluxParticleEditorTestWait.ForDelayCall();
            var nested = Load(outerPath).GetComponentInChildren<FluxParticleAuthoring>();
            Assert.AreEqual(100, nested.ParticleCount);
            Assert.IsFalse(nested.PlayOnAwake);
            var properties = new SerializedObject(nested);
            Assert.IsFalse(properties.FindProperty("particleCount").prefabOverride);
            Assert.IsTrue(properties.FindProperty("playOnAwake").prefabOverride);
            var parent = Load(basePath);
            var parentSettings = new SerializedObject(parent.GetComponent<FluxParticleAuthoring>());
            parentSettings.FindProperty("particleCount").intValue = 600;
            parentSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(parent, basePath);
            var reloaded = Load(outerPath).GetComponentInChildren<FluxParticleAuthoring>();
            Assert.AreEqual(600, reloaded.ParticleCount);
            Assert.IsFalse(reloaded.PlayOnAwake);
        }

        [Test]
        public void RuntimeSerializedSettingsAreHiddenAndSystemRemainsReferenceable()
        {
            foreach (var type in typeof(FluxParticleSystem).Assembly.GetTypes().Where(type => typeof(UdonSharp.UdonSharpBehaviour).IsAssignableFrom(type)))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!field.IsDefined(typeof(SerializeField), false)) continue;
                    Assert.IsTrue(field.IsDefined(typeof(HideInInspector), false), type.Name + "." + field.Name);
                }
            }
            var system = _root.GetComponent<FluxParticleSystem>();
            Assert.AreEqual(HideFlags.None, system.hideFlags & HideFlags.HideInInspector);
            var editor = UnityEditor.Editor.CreateEditor(system);
            try
            {
                Assert.AreEqual("UdonSharpEditor.UdonSharpBehaviourOverrideEditor", editor.GetType().FullName);
                var userEditorField = editor.GetType().GetField("_userEditor", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(userEditorField);
                Assert.IsNull(userEditorField.GetValue(editor), "System must use the default UdonSharp inspector.");
                var inspectorUi = editor.CreateInspectorGUI();
                Assert.IsNotNull(inspectorUi);
                var userUiField = editor.GetType().GetField("_userInspectorElement", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(userUiField);
                var defaultUi = userUiField.GetValue(editor) as UnityEngine.UIElements.IMGUIContainer;
                Assert.IsNotNull(defaultUi);
                Assert.IsNotNull(defaultUi.onGUIHandler);
                Assert.IsTrue(inspectorUi.Contains(defaultUi));
            }
            finally
            {
                Object.DestroyImmediate(editor);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BuildBakesCurrentAndLegacyMainSettings(bool legacy)
        {
            var authoring = _root.GetComponent<FluxParticleAuthoring>();
            if (legacy)
            {
                SetLegacy(_root, 64, false);
            }
            else
            {
                var settings = new SerializedObject(authoring);
                settings.FindProperty("particleCount").intValue = 64;
                settings.FindProperty("playOnAwake").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var system = _root.GetComponent<FluxParticleSystem>();
            using (var session = FluxParticleGenerationSession.Begin("Test", _folder + "/Generated"))
            {
                FluxParticleProgramBuild.Apply(authoring, system, session);
                Assert.AreEqual(64, system.ParticleCount);
                Assert.IsFalse(new SerializedObject(system).FindProperty("playOnAwake").boolValue);
                Assert.AreEqual(legacy ? 0 : 1, new SerializedObject(authoring).FindProperty("settingsVersion").intValue,
                    "Baking must not alter saved authoring migration state.");
            }
        }

        GameObject Load(string path)
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            _loaded.Add(contents);
            return contents;
        }

        static void SetLegacy(GameObject root, int count, bool play)
        {
            SetSystem(root, count, play);
            var settings = new SerializedObject(root.GetComponent<FluxParticleAuthoring>());
            settings.FindProperty("settingsVersion").intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetSystem(GameObject root, int count, bool play)
        {
            var system = new SerializedObject(root.GetComponent<FluxParticleSystem>());
            system.FindProperty("particleCount").intValue = count;
            system.FindProperty("playOnAwake").boolValue = play;
            system.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
