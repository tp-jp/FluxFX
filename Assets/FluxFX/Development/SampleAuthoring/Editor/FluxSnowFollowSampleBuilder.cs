using System;
using System.IO;
using System.Linq;
using TpLab.Flux.FX.Editor.Authoring;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Development.SampleAuthoring.Editor
{
    /// <summary>
    /// 標準機能のみで構成した、プレイヤーの周囲に雪を降らせるサンプルを作成します。
    /// </summary>
    public static class FluxSnowFollowSampleBuilder
    {
        /// <summary>
        /// 開発用サンプルの保存先
        /// </summary>
        public const string Root = "Assets/FluxFX/Samples/Examples/SystemFollow";

        /// <summary>
        /// 降雪確認用のScene
        /// </summary>
        public const string ScenePath = Root + "/SnowFollow.unity";

        /// <summary>
        /// 他のSceneへ配置できる降雪Prefab
        /// </summary>
        public const string PrefabPath = Root + "/Prefabs/SnowFollow.prefab";
        const string SystemPrefab = "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab";
        const string AtlasPath = "Assets/FluxFX/Samples/Examples/RuntimeApi/Textures/ParticleAtlas.png";

        [MenuItem("Tools/FluxFX/Samples/Create Snow Follow Sample")]
        static void CreateFromMenu()
        {
            Create();
            Debug.Log("[FluxFX Samples] Snow Follow: " + ScenePath);
        }

        /// <summary>
        /// サンプルを新規作成します。既存Sceneや生成先Assetを上書きしません。
        /// </summary>
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before creating the sample.");
            if (File.Exists(ScenePath))
            {
                Validate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)?.GetComponent<FluxParticleAuthoring>());
                return;
            }
            var paths = new[] { PrefabPath, Root + "/Materials/Ground.mat", Root + "/Materials/Markers.mat" };
            if (paths.Any(File.Exists)) throw new IOException("Sample assets already exist. Existing files were preserved: " + Root);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SystemPrefab);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (prefab == null || atlas == null) throw new InvalidOperationException("The FluxFX System prefab and sample ParticleAtlas are required.");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var ground = CreateMaterial(paths[1], new Color(0.09f, 0.13f, 0.18f));
                var markers = CreateMaterial(paths[2], new Color(0.28f, 0.39f, 0.48f));
                CreateEnvironment(ground, markers);
                var snow = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                snow.name = "Snow Follow";
                snow.transform.position = Vector3.up * 4.5f;
                Configure(snow.GetComponent<FluxParticleAuthoring>(), atlas);
                foreach (var proxy in snow.GetComponentsInChildren<UdonSharpBehaviour>(true))
                {
                    UdonSharpEditorUtility.CopyProxyToUdon(proxy);
                }
                Validate(snow.GetComponent<FluxParticleAuthoring>());
                if (PrefabUtility.SaveAsPrefabAssetAndConnect(snow, PrefabPath, InteractionMode.AutomatedAction) == null)
                {
                    throw new IOException("Could not save snow prefab: " + PrefabPath);
                }
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save snow scene: " + ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// サンプルが標準の追従・降雪設定を保持しているか検証します。
        /// </summary>
        /// <param name="authoring">検証対象のサンプル設定</param>
        public static void Validate(FluxParticleAuthoring authoring)
        {
            if (authoring == null) throw new InvalidOperationException("Snow authoring is missing.");
            FluxParticleAuthoringValidator.Validate(authoring);
            if (authoring.SystemFollow.Target != FluxParticleFollowTarget.LocalPlayerPosition ||
                authoring.SimulationSpace != FluxParticleSimulationSpace.World || !authoring.PlayOnAwake ||
                authoring.Emission.Rate <= 0 || authoring.Render.Texture == null ||
                !authoring.Modules.OfType<FluxParticleVelocityOverLifetimeModule>().Any(module => module.Start.y < 0 && module.End.y < 0))
            {
                throw new InvalidOperationException("Snow sample follow, emission or downward velocity settings are missing.");
            }
        }

        static void Configure(FluxParticleAuthoring authoring, Texture2D atlas)
        {
            var settings = new SerializedObject(authoring);
            settings.FindProperty("settingsVersion").intValue = 1;
            settings.FindProperty("particleCount").intValue = 4096;
            settings.FindProperty("playOnAwake").boolValue = true;
            settings.FindProperty("simulationSpace").intValue = (int)FluxParticleSimulationSpace.World;
            settings.FindProperty("effectScale").floatValue = 1;
            settings.FindProperty("systemFollow.target").intValue = (int)FluxParticleFollowTarget.LocalPlayerPosition;
            settings.FindProperty("systemFollow.positionOffset").vector3Value = Vector3.up * 4.5f;
            settings.FindProperty("systemFollow.offsetSpace").intValue = (int)FluxParticleFollowOffsetSpace.World;
            settings.FindProperty("systemFollow.rotation").intValue = (int)FluxParticleFollowRotation.None;
            settings.FindProperty("playback.loop").boolValue = true;
            settings.FindProperty("emission.rate").floatValue = 450;
            settings.FindProperty("emission.bursts").arraySize = 0;
            settings.FindProperty("lifetime.mode").intValue = (int)FluxParticleStartLifetimeMode.Constant;
            settings.FindProperty("lifetime.lifetime").floatValue = 8;
            settings.FindProperty("shape.type").intValue = (int)FluxParticleShapeType.Box;
            settings.FindProperty("shape.size").vector3Value = new Vector3(24, 8, 24);
            settings.FindProperty("initialVelocity.speed").floatValue = 0;
            settings.FindProperty("render.texture").objectReferenceValue = atlas;
            // アトラス左下の円形セルのみを使用し、アニメーションで隣の図形へ切り替えない。
            settings.FindProperty("render.tiling").vector2Value = Vector2.one * 0.5f;
            settings.FindProperty("render.offset").vector2Value = Vector2.zero;
            settings.FindProperty("render.blendMode").intValue = (int)FluxParticleBlendMode.Alpha;
            settings.FindProperty("render.startColor").colorValue = Color.white;
            settings.FindProperty("render.intensity").floatValue = 1;
            settings.FindProperty("render.startSizeMode").intValue = (int)FluxParticleStartSizeMode.RandomBetweenTwoConstants;
            settings.FindProperty("render.startSizeMin").floatValue = 0.45f;
            settings.FindProperty("render.startSizeMax").floatValue = 0.9f;
            settings.FindProperty("render.cullingBounds").boundsValue = new Bounds(Vector3.down * 4.5f, new Vector3(160, 40, 160));
            var modules = settings.FindProperty("modules");
            modules.arraySize = 3;
            modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleVelocityOverLifetimeModule();
            modules.GetArrayElementAtIndex(1).managedReferenceValue = new FluxParticleNoiseModule();
            modules.GetArrayElementAtIndex(2).managedReferenceValue = new FluxParticleColorOverLifetimeModule();
            var velocity = modules.GetArrayElementAtIndex(0);
            velocity.FindPropertyRelative("start").vector3Value = new Vector3(0.15f, -1.1f, 0.05f);
            velocity.FindPropertyRelative("end").vector3Value = new Vector3(0.15f, -1.1f, 0.05f);
            var noise = modules.GetArrayElementAtIndex(1);
            noise.FindPropertyRelative("strength").floatValue = 0.035f;
            noise.FindPropertyRelative("scale").floatValue = 0.45f;
            noise.FindPropertyRelative("speed").floatValue = 0.6f;
            var color = modules.GetArrayElementAtIndex(2);
            color.FindPropertyRelative("mode").intValue = (int)FluxParticleColorOverLifetimeMode.Gradient;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.85f, 0.08f),
                    new GradientAlphaKey(0.85f, 0.8f), new GradientAlphaKey(0, 1) });
            color.FindPropertyRelative("gradient").gradientValue = gradient;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material CreateMaterial(string path, Color color)
        {
            var material = new Material(Shader.Find("Unlit/Color")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void CreateEnvironment(Material ground, Material markers)
        {
            var descriptor = new GameObject("World").AddComponent<VRCSceneDescriptor>();
            var spawn = new GameObject("Spawn").transform;
            spawn.position = new Vector3(0, 0.1f, -4);
            descriptor.spawns = new[] { spawn };
            var camera = new GameObject("Sample Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 1.6f, -4);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.075f);
            camera.farClipPlane = 200;
            Cube("Ground", new Vector3(0, -0.2f, 0), new Vector3(80, 0.4f, 80), ground);
            var landmarks = new GameObject("Distance markers").transform;
            for (var z = -20; z <= 20; z += 10)
            {
                for (var x = -20; x <= 20; x += 10)
                {
                    if (x == 0 && z == 0) continue;
                    var post = Cube("Marker " + x + ", " + z, new Vector3(x, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), markers);
                    post.transform.SetParent(landmarks, true);
                }
            }
            var sign = new GameObject("Snow Follow instructions").AddComponent<TextMesh>();
            sign.transform.position = new Vector3(-3.5f, 2.4f, 3);
            sign.characterSize = 0.055f;
            sign.fontSize = 48;
            sign.anchor = TextAnchor.UpperLeft;
            sign.color = new Color(0.8f, 0.9f, 1);
            sign.text = "SYSTEM FOLLOW / SNOW\n\nWalk around the markers.\nNew snow follows your position.\nExisting flakes keep falling in world space.\n\nEditor: select Snow Follow, then Preview / Play.\nChoose Scene Camera or a Transform to follow.\nFreeze Follow Position to inspect from outside.";
        }

        static GameObject Cube(string name, Vector3 position, Vector3 size, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = size;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }
    }
}
