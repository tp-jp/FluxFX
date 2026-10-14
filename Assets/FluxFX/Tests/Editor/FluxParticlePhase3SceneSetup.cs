using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Phase 3の実機動作確認用Sceneを作成します。既存のParticle PrefabとSceneFlow BuildPassを使用します。
    /// </summary>
    public static class FluxParticlePhase3SceneSetup
    {
        const string PrefabPath = "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab";
        const string ScenePath = "Assets/FluxFX/Tests/Runtime/Scenes/Phase3Validation.unity";

        [MenuItem("Tools/FluxFX/Tests/Create Phase 3 Validation Scene")]
        static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[FluxFX] Particle Prefab was not found: {PrefabPath}");
                return;
            }
            if (File.Exists(ScenePath))
            {
                if (!EditorUtility.DisplayDialog("FluxFX Phase 3", "Replace the existing Phase3Validation scene?", "Replace", "Cancel")) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateCase(prefab, scene, "01 Baseline", -4.5f, 64, 8.0f, 0.8f, false, false, false);
            CreateCase(prefab, scene, "02 Gravity", -1.5f, 64, 8.0f, 0.8f, true, false, false);
            CreateCase(prefab, scene, "03 Color Over Lifetime", 1.5f, 64, 8.0f, 0.5f, false, true, false);
            CreateCase(prefab, scene, "04 Burst / Overflow", 4.5f, 32, 0.0f, 0.5f, false, false, true);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[FluxFX] Failed to save validation scene: {ScenePath}");
                return;
            }

            Debug.Log($"[FluxFX] Phase 3 validation scene created: {ScenePath}. Run Play or VRChat Build & Test with SceneFlow enabled.");
            ValidateScene();
        }

        [MenuItem("Tools/FluxFX/Tests/Validate Phase 3 Scene Setup")]
        static void ValidateScene()
        {
            var systems = UnityEngine.Object.FindObjectsOfType<FluxParticleSystem>(true);
            var checkedCount = 0;

            foreach (var system in systems)
            {
                var authoring = system.GetComponent<FluxParticleAuthoring>();
                if (authoring == null || !authoring.UseProgramBackend) continue;

                if (system.GetComponentInChildren<FluxParticleEmitter>(true) == null ||
                    system.GetComponentInChildren<FluxParticleRenderer>(true) == null)
                {
                    Debug.LogError($"[FluxFX] Emitter or Renderer is missing: {system.name}", system);
                    return;
                }

                try
                {
                    var program = FluxParticleBuildPipeline.Build(authoring);
                    Debug.Log($"[FluxFX] {system.name}: {program.Program.Program.StateLayout.Pages.Count} Pages / {program.Program.Passes.Count} Passes", system);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[FluxFX] Invalid Program configuration on {system.name}: {exception.Message}", system);
                    return;
                }

                checkedCount++;
            }

            if (checkedCount == 0)
            {
                Debug.LogWarning("[FluxFX] No Particle Systems with Program Backend enabled were found in this Scene.");
                return;
            }
            Debug.Log($"[FluxFX] Phase 3 Scene setup validation passed: {checkedCount} systems. GPU execution and rendering require Play / Build & Test.");
        }

        static void CreateCase(GameObject prefab, Scene scene, string caseName, float x, int capacity,
            float emissionRate, float speed, bool gravity, bool colorOverLifetime, bool overflow)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = caseName;
            instance.transform.position = new Vector3(x, 0.25f, 0);

            var system = instance.GetComponent<FluxParticleSystem>();
            var authoring = instance.GetComponent<FluxParticleAuthoring>();
            if (system == null || authoring == null)
            {
                throw new InvalidOperationException($"Particle Prefab must contain FluxParticleSystem and FluxParticleAuthoring: {PrefabPath}");
            }

            var systemSerialized = new SerializedObject(system);
            systemSerialized.FindProperty("particleCount").intValue = capacity;
            systemSerialized.FindProperty("playOnAwake").boolValue = true;
            systemSerialized.ApplyModifiedPropertiesWithoutUndo();

            var authoringSerialized = new SerializedObject(authoring);
            authoringSerialized.FindProperty("useProgramBackend").boolValue = true;
            authoringSerialized.FindProperty("simulationSpace").enumValueIndex = (int)FluxParticleSimulationSpace.Local;
            authoringSerialized.FindProperty("shape").FindPropertyRelative("type").enumValueIndex = (int)FluxParticleShapeType.Point;
            authoringSerialized.FindProperty("emission").FindPropertyRelative("rate").floatValue = emissionRate;
            var initialVelocity = authoringSerialized.FindProperty("initialVelocity");
            initialVelocity.FindPropertyRelative("mode").enumValueIndex = (int)FluxParticleStartSpeedMode.Constant;
            initialVelocity.FindPropertyRelative("speed").floatValue = speed;
            authoringSerialized.FindProperty("lifetime").FindPropertyRelative("lifetime").floatValue = 3.0f;
            authoringSerialized.FindProperty("render").FindPropertyRelative("startSize").floatValue = 0.35f;
            authoringSerialized.FindProperty("playback").FindPropertyRelative("duration").floatValue = 6.0f;
            authoringSerialized.FindProperty("playback").FindPropertyRelative("loop").boolValue = true;

            var render = authoringSerialized.FindProperty("render");
            render.FindPropertyRelative("mode").enumValueIndex = (int)FluxParticleRenderMode.Billboard;
            render.FindPropertyRelative("material").objectReferenceValue = null;
            render.FindPropertyRelative("startColorMode").enumValueIndex = (int)FluxParticleStartColorMode.Constant;
            render.FindPropertyRelative("startSizeMode").enumValueIndex = (int)FluxParticleStartSizeMode.Constant;
            render.FindPropertyRelative("startRotation").vector3Value = Vector3.zero;
            render.FindPropertyRelative("blendMode").enumValueIndex = (int)FluxParticleBlendMode.Alpha;
            render.FindPropertyRelative("startColor").colorValue = colorOverLifetime
                ? new Color(1.0f, 0.55f, 0.12f, 1.0f) : Color.white;

            if (overflow)
            {
                var bursts = authoringSerialized.FindProperty("emission").FindPropertyRelative("bursts");
                bursts.arraySize = 1;
                bursts.GetArrayElementAtIndex(0).FindPropertyRelative("time").floatValue = 0.25f;
                bursts.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 96;
            }
            authoringSerialized.ApplyModifiedPropertiesWithoutUndo();

            var modules = new List<FluxParticleModule>();
            if (gravity)
            {
                modules.Add(new FluxParticleGravityModule());
            }
            if (colorOverLifetime)
            {
                var module = new FluxParticleColorOverLifetimeModule();
                typeof(FluxParticleColorOverLifetimeModule).GetField("endColor", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(module, new Color(0.1f, 0.6f, 1.0f, 0.1f));
                modules.Add(module);
            }
            typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(authoring, modules);

            EditorUtility.SetDirty(system);
            EditorUtility.SetDirty(authoring);
            PrefabUtility.RecordPrefabInstancePropertyModifications(system);
            PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
            CreateLabel(caseName, x);
        }

        static void CreateCamera()
        {
            var go = new GameObject("Phase 3 Validation Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 1.0f, -18.0f);
            go.transform.rotation = Quaternion.identity;
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
        }

        static void CreateLabel(string caseName, float x)
        {
            var go = new GameObject(caseName + " Label");
            go.transform.position = new Vector3(x, 3.3f, 0);
            var label = go.AddComponent<TextMesh>();
            label.text = caseName;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.035f;
            label.fontSize = 64;
            label.color = Color.white;
        }
    }
}
