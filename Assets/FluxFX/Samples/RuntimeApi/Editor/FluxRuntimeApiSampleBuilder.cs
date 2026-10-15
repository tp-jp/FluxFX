using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TpLab.Flux.FX.CustomModules;
using TpLab.Flux.FX.Samples.RuntimeApi.Runtime;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Samples.RuntimeApi.Editor
{
    /// <summary>
    /// Runtime APIを操作するUIと、形状別のAuthoring設定を作成します。
    /// </summary>
    public static class FluxRuntimeApiSampleBuilder
    {
        public const string Root = "Assets/FluxFX/Samples/RuntimeApi";
        public const string ScenePath = Root + "/RuntimeApiPlayground.unity";
        const int Capacity = 2048;
        static readonly Color Background = new Color(0.035f, 0.055f, 0.09f, 0.98f);
        static readonly Color Accent = new Color(0.2f, 0.85f, 0.75f);
        static DefaultControls.Resources Resources => new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
        static Font Font => UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        readonly struct ControlSpec
        {
            public readonly int Page;
            public readonly SampleControl Id;
            public readonly string Label;
            public readonly float Min, Max, Default;
            public readonly bool Integer;

            public ControlSpec(int page, SampleControl id, string label, float min, float max, float initial, bool integer)
            {
                Page = page;
                Id = id;
                Label = label;
                Min = min;
                Max = max;
                Default = initial;
                Integer = integer;
            }
        }

        static readonly ControlSpec[] Specs =
        {
            new ControlSpec(0, SampleControl.Lifetime, "Lifetime (seconds) [initial]", 0.2f, 10f, 3f, false),
            new ControlSpec(0, SampleControl.Speed, "Start speed", 0f, 8f, 2f, false),
            new ControlSpec(0, SampleControl.StartSize, "Start size [initial]", 0.2f, 12f, 3.5f, false),
            new ControlSpec(0, SampleControl.StartR, "Start color / R [initial]", 0f, 1f, 0.15f, false),
            new ControlSpec(0, SampleControl.StartG, "Start color / G [initial]", 0f, 1f, 0.75f, false),
            new ControlSpec(0, SampleControl.StartB, "Start color / B [initial]", 0f, 1f, 1f, false),
            new ControlSpec(0, SampleControl.StartA, "Start color / A [initial]", 0f, 1f, 1f, false),
            new ControlSpec(0, SampleControl.StartRotation, "Start rotation / Z [initial]", -180f, 180f, 0f, false),
            new ControlSpec(1, SampleControl.Rate, "Particles per second", 0f, 500f, 100f, false),
            new ControlSpec(1, SampleControl.Duration, "Duration (restart)", 0.1f, 10f, 5f, false),
            new ControlSpec(1, SampleControl.Delay, "Start delay (restart)", 0f, 5f, 0f, false),
            new ControlSpec(1, SampleControl.BurstTime, "Burst time (restart)", 0f, 5f, 0f, false),
            new ControlSpec(1, SampleControl.BurstCount, "Burst count (restart)", 0f, 250f, 20f, true),
            new ControlSpec(2, SampleControl.PositionX, "Offset / X", -3f, 3f, 0f, false),
            new ControlSpec(2, SampleControl.PositionY, "Offset / Y", -3f, 3f, 0f, false),
            new ControlSpec(2, SampleControl.PositionZ, "Offset / Z", -3f, 3f, 0f, false),
            new ControlSpec(2, SampleControl.Radius, "Radius", 0f, 3f, 0.7f, false),
            new ControlSpec(2, SampleControl.Angle, "Cone angle", 0f, 85f, 25f, false),
            new ControlSpec(2, SampleControl.BoxX, "Box size / X", 0f, 5f, 2f, false),
            new ControlSpec(2, SampleControl.BoxY, "Box size / Y", 0f, 5f, 2f, false),
            new ControlSpec(2, SampleControl.BoxZ, "Box size / Z", 0f, 5f, 2f, false),
            new ControlSpec(3, SampleControl.GravityX, "Gravity / X", -10f, 10f, 0f, false),
            new ControlSpec(3, SampleControl.GravityY, "Gravity / Y", -10f, 10f, -1f, false),
            new ControlSpec(3, SampleControl.GravityZ, "Gravity / Z", -10f, 10f, 0f, false),
            new ControlSpec(3, SampleControl.Drag, "Drag", 0f, 5f, 0.3f, false),
            new ControlSpec(3, SampleControl.ForceX, "Force / X", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.ForceY, "Force / Y", -5f, 5f, 1f, false),
            new ControlSpec(3, SampleControl.ForceZ, "Force / Z", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.NoiseStrength, "Noise / strength", 0f, 5f, 1f, false),
            new ControlSpec(3, SampleControl.NoiseScale, "Noise / scale", 0f, 5f, 1f, false),
            new ControlSpec(3, SampleControl.NoiseSpeed, "Noise / speed", 0f, 5f, 1f, false),
            new ControlSpec(3, SampleControl.VortexStrength, "Vortex / strength", 0f, 8f, 2f, false),
            new ControlSpec(3, SampleControl.VortexAxisX, "Vortex axis / X", -1f, 1f, 0f, false),
            new ControlSpec(3, SampleControl.VortexAxisY, "Vortex axis / Y", -1f, 1f, 1f, false),
            new ControlSpec(3, SampleControl.VortexAxisZ, "Vortex axis / Z", -1f, 1f, 0f, false),
            new ControlSpec(3, SampleControl.VortexCenterX, "Vortex center / X", -3f, 3f, 0f, false),
            new ControlSpec(3, SampleControl.VortexCenterY, "Vortex center / Y", -3f, 3f, 0f, false),
            new ControlSpec(3, SampleControl.VortexCenterZ, "Vortex center / Z", -3f, 3f, 0f, false),
            new ControlSpec(3, SampleControl.MaxSpeed, "Limit velocity / max speed", 0f, 10f, 3f, false),
            new ControlSpec(3, SampleControl.VelocityStartX, "Velocity start / X", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.VelocityStartY, "Velocity start / Y", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.VelocityStartZ, "Velocity start / Z", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.VelocityEndX, "Velocity end / X", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.VelocityEndY, "Velocity end / Y", -5f, 5f, 2f, false),
            new ControlSpec(3, SampleControl.VelocityEndZ, "Velocity end / Z", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.OrbitalX, "Orbital / X", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.OrbitalY, "Orbital / Y", -5f, 5f, 1f, false),
            new ControlSpec(3, SampleControl.OrbitalZ, "Orbital / Z", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.OrbitOffsetX, "Orbit offset / X", -3f, 3f, 0f, false),
            new ControlSpec(3, SampleControl.OrbitOffsetY, "Orbit offset / Y", -3f, 3f, 0f, false),
            new ControlSpec(3, SampleControl.OrbitOffsetZ, "Orbit offset / Z", -3f, 3f, 0f, false),
            new ControlSpec(3, SampleControl.Radial, "Radial velocity", -5f, 5f, 0f, false),
            new ControlSpec(3, SampleControl.LiftStrength, "Custom Lift / strength", 0f, 8f, 2f, false),
            new ControlSpec(3, SampleControl.LiftX, "Custom Lift direction / X", -1f, 1f, 0f, false),
            new ControlSpec(3, SampleControl.LiftY, "Custom Lift direction / Y", -1f, 1f, 1f, false),
            new ControlSpec(3, SampleControl.LiftZ, "Custom Lift direction / Z", -1f, 1f, 0f, false),
            new ControlSpec(4, SampleControl.EndR, "Color over lifetime / R", 0f, 1f, 1f, false),
            new ControlSpec(4, SampleControl.EndG, "Color over lifetime / G", 0f, 1f, 0.3f, false),
            new ControlSpec(4, SampleControl.EndB, "Color over lifetime / B", 0f, 1f, 0.1f, false),
            new ControlSpec(4, SampleControl.EndA, "Color over lifetime / A", 0f, 1f, 0f, false),
            new ControlSpec(4, SampleControl.EndSize, "Size over lifetime / end", 0f, 8f, 0.3f, false),
            new ControlSpec(4, SampleControl.EndRotation, "Rotation over lifetime / Z", -720f, 720f, 180f, false),
            new ControlSpec(4, SampleControl.Cycles, "Texture sheet / cycles", 0f, 8f, 1f, false),
            new ControlSpec(4, SampleControl.TilesX, "Texture sheet / columns [initial]", 1f, 4f, 2f, true),
            new ControlSpec(4, SampleControl.TilesY, "Texture sheet / rows [initial]", 1f, 4f, 2f, true),
        };

        [MenuItem("Tools/FluxFX/Samples/Open Runtime API Playground")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>
        /// Authoring設定・UI・素材を含むサンプルSceneを新規作成します。
        /// </summary>
        public static void Create(Action completed)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before creating the sample.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Sample scene already exists: " + ScenePath);
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Textures");
            AssetDatabase.Refresh();
            EnsureProgram();
            File.Copy(Root + "/Editor/EmptyScene.txt", ScenePath);
            AssetDatabase.ImportAsset(ScenePath);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var camera = new GameObject("Sample Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 2.8f, -10);
                camera.fieldOfView = 35;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Background;
                var descriptor = new GameObject("World").AddComponent<VRCSceneDescriptor>();
                var spawn = new GameObject("Spawn").transform;
                spawn.position = new Vector3(0, 0, -8);
                descriptor.spawns = new[] { spawn };
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Floor";
                floor.transform.position = new Vector3(0, -0.15f, 2);
                floor.transform.localScale = new Vector3(30, 0.2f, 30);
                var floorMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.07f, 0.1f, 0.15f) };
                AssetDatabase.CreateAsset(floorMaterial, Root + "/Materials/Floor.mat");
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                var controller = new GameObject("Runtime API Controls").AddUdonSharpComponent<FluxRuntimeApiSample>();
                var material = CreateParticleMaterial();
                var systems = new FluxParticleSystem[Enum.GetValues(typeof(FluxParticleShapeType)).Length];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab");
                for (var i = 0; i < systems.Length; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = "Particles - " + (FluxParticleShapeType)i;
                    instance.transform.position = new Vector3(1.7f, 1.8f, 1.5f);
                    var system = instance.GetComponent<FluxParticleSystem>();
                    system.SetProgramVariable("particleCount", Capacity);
                    system.SetProgramVariable("playOnAwake", false);
                    var authoring = instance.GetComponent<FluxParticleAuthoring>();
                    ConfigureAuthoring(authoring, i, material);
                    systems[i] = system;
                }
                controller.SetProgramVariable("systems", systems);
                CreateUi(controller, camera);
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            foreach (var proxy in root.GetComponentsInChildren<UdonSharpBehaviour>(true))
                            {
                                UdonSharpEditorUtility.CopyProxyToUdon(proxy);
                            }
                        }
                        ValidateAuthoring(systems, controller);
                        EditorSceneManager.SaveScene(scene);
                        AssetDatabase.SaveAssets();
                        Capture(camera);
                        EditorSceneManager.CloseScene(scene, true);
                        completed?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        File.WriteAllText("Temp/RuntimeSampleResult.txt", exception.ToString());
                    }
                };
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
            }
        }

        static void EnsureProgram()
        {
            var path = Root + "/Runtime/FluxRuntimeApiSample.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                asset.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "/Runtime/FluxRuntimeApiSample.cs");
                AssetDatabase.CreateAsset(asset, path);
                typeof(UdonSharpProgramAsset).GetMethod("ClearProgramAssetCache", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
            }
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = true });
            if (asset.SerializedProgramAsset.RetrieveProgram() == null) throw new InvalidOperationException("Sample Udon program did not compile.");
        }

        static void ConfigureAuthoring(FluxParticleAuthoring authoring, int shape, Material material)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("shape.type").intValue = shape;
            serialized.FindProperty("shape.radius").floatValue = 0.7f;
            serialized.FindProperty("shape.size").vector3Value = Vector3.one * 2;
            serialized.FindProperty("initialVelocity.speed").floatValue = 2;
            serialized.FindProperty("emission.rate").floatValue = 100;
            serialized.FindProperty("render.material").objectReferenceValue = material;
            serialized.FindProperty("render.blendMode").intValue = (int)FluxParticleBlendMode.Alpha;
            serialized.FindProperty("render.startSize").floatValue = 3.5f;
            serialized.FindProperty("render.startColor").colorValue = new Color(0.15f, 0.75f, 1);
            var definitions = new FluxParticleModule[]
            {
                new FluxParticleGravityModule(), new FluxParticleDragModule(), new FluxParticleForceModule(),
                new FluxParticleNoiseModule(), new FluxParticleVortexModule(), new FluxParticleLimitVelocityModule(),
                new FluxParticleVelocityOverLifetimeModule(), new FluxParticleColorOverLifetimeModule(),
                new FluxParticleSizeOverLifetimeModule(), new FluxParticleRotationOverLifetimeModule(),
                new FluxParticleTextureSheetAnimationModule(), new CustomLiftModule()
            };
            var modules = serialized.FindProperty("modules");
            modules.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                modules.GetArrayElementAtIndex(i).managedReferenceValue = definitions[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material CreateParticleMaterial()
        {
            const int size = 128;
            var atlas = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var uv = new Vector2((x % 64 + 0.5f) / 32 - 1, (y % 64 + 0.5f) / 32 - 1);
                    var cell = x / 64 + y / 64 * 2;
                    var radius = uv.magnitude;
                    var distance = cell == 0 ? radius : cell == 1 ? Mathf.Abs(radius - 0.55f) * 4
                        : cell == 2 ? Mathf.Abs(uv.x) + Mathf.Abs(uv.y) : Mathf.Min(Mathf.Abs(uv.x), Mathf.Abs(uv.y)) * 4 + radius * 0.4f;
                    pixels[x + y * size] = new Color(1, 1, 1, Mathf.Clamp01((0.8f - distance) * 12));
                }
            }
            atlas.SetPixels(pixels);
            atlas.Apply();
            var texturePath = Root + "/Textures/ParticleAtlas.png";
            File.WriteAllBytes(texturePath, atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var material = new Material(Shader.Find("FluxFX/ParticleMaterial"));
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            AssetDatabase.CreateAsset(material, Root + "/Materials/Particles.mat");
            return material;
        }

        static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, x, y, width, height);
            return rect;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static Text Label(Transform parent, string content, float x, float y, float width, float height, int size = 18)
        {
            var text = Rect(content, parent, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = Color.white;
            text.text = content;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        static Button Button(Transform parent, string label, float x, float y, float width, UdonBehaviour target, string action)
        {
            var rect = Rect(label, parent, x, y, width, 40);
            rect.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.23f, 0.3f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Label(rect, label, 8, 0, width - 16, 40).alignment = TextAnchor.MiddleCenter;
            UnityEventTools.AddStringPersistentListener(button.onClick, target.SendCustomEvent, action);
            return button;
        }

        static Toggle Toggle(Transform parent, string label, float x, float y, bool enabled, UdonBehaviour target)
        {
            var go = DefaultControls.CreateToggle(Resources);
            go.name = label;
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, 220, 30);
            var toggle = go.GetComponent<Toggle>();
            toggle.isOn = enabled;
            foreach (var text in go.GetComponentsInChildren<Text>())
            {
                text.font = Font;
                text.fontSize = 17;
                text.color = Color.white;
                text.text = label;
            }
            UnityEventTools.AddStringPersistentListener(toggle.onValueChanged, target.SendCustomEvent, nameof(FluxRuntimeApiSample.OnControlsChanged));
            return toggle;
        }

        static void CreateUi(FluxRuntimeApiSample controller, Camera camera)
        {
            var canvas = new GameObject("Runtime API UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(VRCUiShape)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            var rect = (RectTransform)canvas.transform;
            rect.sizeDelta = new Vector2(1280, 900);
            rect.position = new Vector3(0, 2.8f, 0);
            rect.localScale = Vector3.one * 0.006f;
            var panel = Rect("Controls", rect, 0, 0, 510, 900);
            panel.gameObject.AddComponent<Image>().color = Background;
            Label(panel, "FluxFX  /  Runtime API", 24, 16, 465, 44, 27).color = Accent;
            Label(panel, "Main, Shape & Module handles", 24, 59, 465, 28, 17);
            var target = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
            var play = Button(panel, "Pause", 24, 100, 140, target, nameof(FluxRuntimeApiSample.TogglePlay));
            Button(panel, "Restart", 178, 100, 140, target, nameof(FluxRuntimeApiSample.Restart));
            Button(panel, "Stop", 332, 100, 150, target, nameof(FluxRuntimeApiSample.Stop));
            var dropdown = DefaultControls.CreateDropdown(Resources).GetComponent<Dropdown>();
            dropdown.transform.SetParent(panel, false);
            Place((RectTransform)dropdown.transform, 24, 152, 458, 38);
            dropdown.options = Enum.GetNames(typeof(FluxParticleShapeType)).Select(name => new Dropdown.OptionData(name)).ToList();
            dropdown.value = (int)FluxParticleShapeType.Sphere;
            foreach (var text in dropdown.GetComponentsInChildren<Text>(true))
            {
                text.font = Font;
                text.fontSize = 18;
            }
            dropdown.RefreshShownValue();
            UnityEventTools.AddStringPersistentListener(dropdown.onValueChanged, target.SendCustomEvent, nameof(FluxRuntimeApiSample.OnShapeChanged));
            var pageNames = new[] { "Main", "Emission", "Shape", "Motion", "Render" };
            var pages = new GameObject[pageNames.Length];
            var controls = new Slider[(int)SampleControl.Count];
            var values = new Text[controls.Length];
            var toggles = new Toggle[8];
            Toggle loop = null;
            for (var p = 0; p < pages.Length; p++)
            {
                Button(panel, pageNames[p], 24 + p * 93, 204, 88, target, "Show" + pageNames[p]).GetComponentInChildren<Text>().fontSize = 15;
                var scroll = DefaultControls.CreateScrollView(Resources).GetComponent<ScrollRect>();
                scroll.name = pageNames[p];
                scroll.transform.SetParent(panel, false);
                Place((RectTransform)scroll.transform, 16, 260, 478, 490);
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f);
                scroll.horizontalScrollbar.gameObject.SetActive(false);
                scroll.content.anchorMin = new Vector2(0, 1);
                scroll.content.anchorMax = new Vector2(1, 1);
                scroll.content.pivot = new Vector2(0.5f, 1);
                var y = 12f;
                if (p == 1)
                {
                    loop = Toggle(scroll.content, "Loop (restart)", 12, y, true, target);
                    y += 40;
                }
                if (p == 3)
                {
                    var names = new[] { "Gravity", "Drag", "Force", "Noise", "Vortex", "Limit velocity", "Velocity over lifetime", "Custom Lift" };
                    for (var i = 0; i < names.Length; i++)
                    {
                        toggles[i] = Toggle(scroll.content, names[i], 12 + i % 2 * 220, y + i / 2 * 36, i == 3, target);
                    }
                    y += 160;
                }
                foreach (var spec in Specs.Where(spec => spec.Page == p))
                {
                    Label(scroll.content, spec.Label, 12, y, 350, 26, 16);
                    var value = Label(scroll.content, spec.Default.ToString(spec.Integer ? "F0" : "F2"), 366, y, 65, 26, 16);
                    value.color = Accent;
                    var slider = DefaultControls.CreateSlider(Resources).GetComponent<Slider>();
                    slider.transform.SetParent(scroll.content, false);
                    Place((RectTransform)slider.transform, 16, y + 32, 414, 20);
                    slider.minValue = spec.Min;
                    slider.maxValue = spec.Max;
                    slider.wholeNumbers = spec.Integer;
                    slider.value = spec.Default;
                    slider.fillRect.GetComponent<Image>().color = Accent;
                    UnityEventTools.AddStringPersistentListener(slider.onValueChanged, target.SendCustomEvent, nameof(FluxRuntimeApiSample.OnControlsChanged));
                    controls[(int)spec.Id] = slider;
                    values[(int)spec.Id] = value;
                    y += 72;
                }
                scroll.content.sizeDelta = new Vector2(0, y);
                pages[p] = scroll.gameObject;
                pages[p].SetActive(p == 0);
            }
            Label(panel, "[initial]: Apply & Restart   |   (restart): Restart", 24, 758, 465, 28, 16);
            Button(panel, "Apply initial values & Restart", 24, 795, 458, target, nameof(FluxRuntimeApiSample.ApplyInitialValues));
            var message = Label(panel, "Ready to play", 24, 842, 458, 48, 16);
            Label(rect, "PARTICLE PLAYGROUND", 558, 24, 680, 45, 28).color = Accent;
            var status = Label(rect, "Sphere / 2048 slots", 558, 74, 680, 60, 20);
            Label(rect, "Play the scene. In ClientSim, hold Tab to use the UI.\nChanges are local. Drag sliders to compare behaviour.", 558, 804, 690, 78, 18);
            controller.SetProgramVariable("controls", controls);
            controller.SetProgramVariable("values", values);
            controller.SetProgramVariable("moduleToggles", toggles);
            controller.SetProgramVariable("loopToggle", loop);
            controller.SetProgramVariable("shapeSelector", dropdown);
            controller.SetProgramVariable("pages", pages);
            controller.SetProgramVariable("status", status);
            controller.SetProgramVariable("message", message);
            controller.SetProgramVariable("playLabel", play.GetComponentInChildren<Text>());
            ConfigureSelectionControls(canvas);
        }

        /// <summary>
        /// 選択肢の行高とチェック表示を設定します。
        /// </summary>
        public static void ConfigureSelectionControls(Canvas canvas)
        {
            foreach (var toggle in canvas.GetComponentsInChildren<Toggle>(true))
            {
                var background = (Image)toggle.targetGraphic;
                background.sprite = Resources.standard;
                background.type = Image.Type.Sliced;
                var checkmark = (Image)toggle.graphic;
                checkmark.sprite = Resources.checkmark;
                checkmark.color = new Color(0.05f, 0.5f, 0.4f);
            }
            foreach (var dropdown in canvas.GetComponentsInChildren<Dropdown>(true))
            {
                dropdown.transform.Find("Arrow").GetComponent<Image>().sprite = Resources.dropdown;
                dropdown.transform.Find("Arrow").GetComponent<Image>().color = new Color(0.1f, 0.2f, 0.25f);
                var item = dropdown.itemText.GetComponentInParent<Toggle>(true).GetComponent<RectTransform>();
                item.sizeDelta = new Vector2(item.sizeDelta.x, 32);
                ((RectTransform)item.parent).sizeDelta = new Vector2(0, 40);
                dropdown.template.sizeDelta = new Vector2(0, 210);
                dropdown.itemText.font = Font;
                dropdown.itemText.fontSize = 18;
                dropdown.itemText.color = new Color(0.1f, 0.15f, 0.2f);
                dropdown.itemText.horizontalOverflow = HorizontalWrapMode.Overflow;
                dropdown.itemText.verticalOverflow = VerticalWrapMode.Overflow;
                dropdown.itemText.rectTransform.offsetMin = new Vector2(30, 2);
                dropdown.itemText.rectTransform.offsetMax = new Vector2(-10, -2);
            }
        }

        static void ValidateAuthoring(FluxParticleSystem[] systems, FluxRuntimeApiSample controller)
        {
            foreach (var system in systems)
            {
                var authoring = system.GetComponent<FluxParticleAuthoring>();
                if (authoring == null || authoring.Render.Material == null)
                {
                    throw new InvalidOperationException("Authoring or source Material is missing: " + system.name);
                }
            }
            var controls = (Slider[])controller.GetProgramVariable("controls");
            if (controls.Length != (int)SampleControl.Count || controls.Any(control => control == null || control.onValueChanged.GetPersistentEventCount() != 1))
            {
                throw new InvalidOperationException("Sample controls are not connected.");
            }
        }

        static void Capture(Camera camera)
        {
            Canvas.ForceUpdateCanvases();
            var texture = new RenderTexture(1600, 1100, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(1600, 1100, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 1100), 0, 0);
            image.Apply();
            File.WriteAllBytes("Temp/RuntimeApiSample.png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(texture);
        }
    }
}

