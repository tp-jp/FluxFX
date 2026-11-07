using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TpLab.Flux.FX.Editor.Build;
using TpLab.Flux.FX.Samples.RuntimeApi.Udon;
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
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon;
using Object = UnityEngine.Object;
using Ui = TpLab.Flux.FX.Development.SampleAuthoring.Editor.FluxRuntimeApiSampleBuilder;

namespace TpLab.Flux.FX.Development.SampleAuthoring.Editor
{
    /// <summary>
    /// 既存サンプルの環境を使って同期版と100万Particle版を作成・検証します。
    /// </summary>
    public static class FluxSampleVariantsBuilder
    {
        const string Root = FluxRuntimeApiSampleBuilder.Root;
        const string SyncedPath = Root + "/SyncedRuntimeApi.unity";
        const string MillionPath = Root + "/MillionParticles.unity";
        const string ResultPath = "Temp/FluxFXTests/SampleVariants.txt";
        static readonly string[] SharedEvents =
        {
            "OnControlsChanged", "OnShapeChanged", "ApplyInitialValues", "TogglePlay", "Stop", "Restart"
        };
        static bool _running;

        [MenuItem("Tools/FluxFX/Samples/Create Sample Variations")]
        static void Create()
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            var report = new StringBuilder(DateTime.Now.ToString("O") + "\n");
            _running = true;
            try
            {
                EnsureProgram("FluxRuntimeApiSampleSync");
                EnsureProgram("FluxMillionParticlesSample");
                Compile(false, report);
                Compile(true, report);
                CreateNext(SyncedPath, true, report, () =>
                    CreateNext(MillionPath, false, report, () =>
                    {
                        report.AppendLine("PASS: sample scenes saved; no generated runtime hierarchy baked into scenes.");
                        File.WriteAllText(ResultPath, report.ToString());
                        _running = false;
                        Debug.Log("[FluxFX Samples] Completed: " + ResultPath);
                    }));
            }
            catch (Exception exception)
            {
                Fail(report, exception);
            }
        }

        static void EnsureProgram(string name)
        {
            var path = Root + "/Udon/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) != null) return;
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "/Udon/" + name + ".cs");
            if (script == null) throw new InvalidOperationException("Script is not imported: " + name);
            var program = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            program.sourceCsScript = script;
            AssetDatabase.CreateAsset(program, path);
            typeof(UdonSharpProgramAsset).GetMethod("ClearProgramAssetCache", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }

        static void Compile(bool editor, StringBuilder report)
        {
            var errors = new List<string>();
            void Log(string message, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errors.Add(message);
                }
            }
            Application.logMessageReceived += Log;
            try
            {
                UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = editor });
                foreach (var name in new[] { "FluxRuntimeApiSample", "FluxRuntimeApiSampleSync", "FluxMillionParticlesSample" })
                {
                    var program = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(Root + "/Udon/" + name + ".asset");
                    program.UpdateProgram();
                    if (program.SerializedProgramAsset.RetrieveProgram() == null)
                    {
                        errors.Add("Program unavailable: " + name);
                    }
                }
            }
            finally
            {
                Application.logMessageReceived -= Log;
            }
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            report.AppendLine("PASS: " + (editor ? "Editor" : "Player") + " Udon compile");
            File.WriteAllText(ResultPath, report.ToString());
        }

        static void CreateNext(string path, bool synced, StringBuilder report, Action completed)
        {
            var previous = SceneManager.GetActiveScene();
            var opened = SceneManager.GetSceneByPath(path);
            if (opened.IsValid() && opened.isLoaded)
            {
                Fail(report, new InvalidOperationException("Close the variant scene before generation: " + path));
                return;
            }
            var exists = File.Exists(path);
            if (!exists && !AssetDatabase.CopyAsset(FluxRuntimeApiSampleBuilder.ScenePath, path))
            {
                Fail(report, new IOException("Could not copy source scene: " + path));
                return;
            }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            // PrefabのUdonSharp設定に伴う遅延処理が完了してから構成を編集する。
            EditorApplication.delayCall += () =>
            {
                try
                {
                    SceneManager.SetActiveScene(scene);
                    if (!exists)
                    {
                        ClearBlueprint(scene);
                        if (synced)
                        {
                            CreateSynced(scene);
                        }
                        else
                        {
                            CreateMillion(scene);
                        }
                    }
                    EditorApplication.delayCall += () => FinishScene(scene, previous, path, exists, synced, report, completed);
                }
                catch (Exception exception)
                {
                    RestoreScene(scene, previous);
                    if (!exists)
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                    Fail(report, exception);
                }
            };
        }

        static void FinishScene(Scene scene, Scene previous, string path, bool existed, bool synced, StringBuilder report, Action completed)
        {
            try
            {
                if (!existed)
                {
                    foreach (var proxy in Components<UdonSharpBehaviour>(scene))
                    {
                        UdonSharpEditorUtility.CopyProxyToUdon(proxy);
                    }
                }
                Validate(scene, synced);
                if (!existed && !EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
                AssetDatabase.SaveAssets();
                report.AppendLine("PASS: " + path + (existed ? " (validated existing scene; preserved)" : " (created)"));
                File.WriteAllText(ResultPath, report.ToString());
                RestoreScene(scene, previous);
                completed();
            }
            catch (Exception exception)
            {
                RestoreScene(scene, previous);
                if (!existed)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                Fail(report, exception);
            }
        }

        static void RestoreScene(Scene scene, Scene previous)
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void Fail(StringBuilder report, Exception exception)
        {
            _running = false;
            report.AppendLine("FAIL: " + exception);
            File.WriteAllText(ResultPath, report.ToString());
            Debug.LogException(exception);
        }

        static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        static void ClearBlueprint(Scene scene)
        {
            foreach (var component in Components<MonoBehaviour>(scene))
            {
                if (component == null || component.GetType().Name != "PipelineManager") continue;
                var serialized = new SerializedObject(component);
                var blueprint = serialized.FindProperty("blueprintId");
                if (blueprint != null)
                {
                    blueprint.stringValue = "";
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static void CreateSynced(Scene scene)
        {
            var sample = Components<FluxRuntimeApiSample>(scene).Single();
            var settings = new SerializedObject(sample);
            settings.FindProperty("startPaused").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var selector = (Dropdown)settings.FindProperty("shapeSelector").objectReferenceValue;
            var panel = selector.transform.parent;
            var canvas = selector.GetComponentInParent<Canvas>();
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panel.gameObject.AddComponent<CanvasGroup>();
            }
            group.interactable = false;
            var sync = new GameObject("Shared Runtime API State").AddUdonSharpComponent<FluxRuntimeApiSampleSync>();
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(sync);
            var oldBacking = UdonSharpEditorUtility.GetBackingUdonBehaviour(sample);
            var rewired = 0;
            foreach (var component in canvas.GetComponentsInChildren<Selectable>(true))
            {
                var serialized = new SerializedObject(component);
                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.name != "m_Target" || iterator.propertyType != SerializedPropertyType.ObjectReference || iterator.objectReferenceValue != oldBacking) continue;
                    var parentPath = iterator.propertyPath.Substring(0, iterator.propertyPath.Length - ".m_Target".Length);
                    var argument = serialized.FindProperty(parentPath + ".m_Arguments.m_StringArgument");
                    if (argument == null || !SharedEvents.Contains(argument.stringValue)) continue;
                    iterator.objectReferenceValue = backing;
                    rewired++;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            if (rewired < (int)SampleControl.Count) throw new InvalidOperationException("Shared UI events were not found.");
            var owner = Ui.Label(canvas.transform, "Waiting for shared settings...", 558, 148, 680, 50, 18);
            Ui.Button(canvas.transform, "Take Control", 558, 206, 220, backing, nameof(FluxRuntimeApiSampleSync.TakeControl));
            sync.SetProgramVariable("sample", sample);
            sync.SetProgramVariable("sharedControls", group);
            sync.SetProgramVariable("ownerLabel", owner);
            foreach (var label in canvas.GetComponentsInChildren<Text>(true))
            {
                if (label.text.Contains("Changes are local"))
                {
                    label.text = "Take Control to share settings and playback.\nParticle positions are simulated locally on each client.";
                }
                if (label.text == "PARTICLE PLAYGROUND")
                {
                    label.text = "SHARED PARTICLE PLAYGROUND";
                }
            }
        }

        static void CreateMillion(Scene scene)
        {
            var sample = Components<FluxRuntimeApiSample>(scene).Single();
            var settings = new SerializedObject(sample);
            var systems = Components<FluxParticleSystem>(scene);
            var system = systems.First(value => value.GetComponent<FluxParticleAuthoring>().Shape.Type == FluxParticleShapeType.Sphere);
            foreach (var extra in systems)
            {
                if (extra == system) continue;
                Object.DestroyImmediate(extra.gameObject);
            }
            var oldCanvas = ((Dropdown)settings.FindProperty("shapeSelector").objectReferenceValue).GetComponentInParent<Canvas>();
            var camera = oldCanvas.worldCamera;
            var position = oldCanvas.transform.position;
            var rotation = oldCanvas.transform.rotation;
            var scale = oldCanvas.transform.localScale;
            Object.DestroyImmediate(oldCanvas.gameObject);
            Object.DestroyImmediate(sample.gameObject);
            system.name = "Million Particles";
            system.transform.position = new Vector3(2.8f, 3.2f, 3.5f);
            ConfigureMillion(system.GetComponent<FluxParticleAuthoring>());
            var controller = new GameObject("Million Particle Controls").AddUdonSharpComponent<FluxMillionParticlesSample>();
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
            var canvas = new GameObject("Million Particle UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(VRCUiShape)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            var rect = (RectTransform)canvas.transform;
            rect.sizeDelta = new Vector2(1280, 900);
            rect.position = position;
            rect.rotation = rotation;
            rect.localScale = scale;
            var panel = Ui.Rect("Controls", rect, 0, 0, 510, 900);
            panel.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.98f);
            Ui.Label(panel, "FluxFX / Million Particles", 24, 16, 465, 50, 25);
            Ui.Label(panel, "1 System / Billboard / Noise + Color", 24, 72, 465, 35, 17);
            var play = Ui.Button(panel, "Play", 24, 128, 140, backing, "TogglePlay");
            Ui.Button(panel, "Restart", 178, 128, 140, backing, "Restart");
            Ui.Button(panel, "Stop", 332, 128, 150, backing, "Stop");
            controller.SetProgramVariable("system", system);
            controller.SetProgramVariable("rate", Slider(panel, backing, "Particles per second", 210, 0, 250000, 110000, true, out var rateLabel));
            controller.SetProgramVariable("noise", Slider(panel, backing, "Noise strength", 310, 0, 1, 0.15f, false, out var noiseLabel));
            controller.SetProgramVariable("intensity", Slider(panel, backing, "Intensity", 410, 0, 5, 1, false, out var intensityLabel));
            controller.SetProgramVariable("rateLabel", rateLabel);
            controller.SetProgramVariable("noiseLabel", noiseLabel);
            controller.SetProgramVariable("intensityLabel", intensityLabel);
            controller.SetProgramVariable("status", Ui.Label(panel, "Ready / 1,048,576 slots", 24, 620, 458, 90, 21));
            controller.SetProgramVariable("playLabel", play.GetComponentInChildren<Text>());
            Ui.Label(panel, "Lifetime: 10 s\nPlay to fill the volume. Restart clears particles.\nStop ends emission; existing particles expire.", 24, 752, 458, 128, 17);
            Ui.Label(rect, "1,048,576 PARTICLES", 558, 24, 690, 60, 30);
            Ui.Label(rect, "Local visual sample. Performance depends on your GPU.\nIn ClientSim, hold Tab to use the UI.", 558, 804, 690, 78, 18);
        }

        static Slider Slider(Transform parent, UdonBehaviour target, string label, float y, float min, float max, float value, bool integer, out Text valueLabel)
        {
            valueLabel = Ui.Label(parent, label + ": " + value.ToString(integer ? "N0" : "F2"), 24, y, 450, 30, 19);
            var slider = DefaultControls.CreateSlider(Ui.Resources).GetComponent<Slider>();
            slider.name = label;
            slider.transform.SetParent(parent, false);
            Ui.Place((RectTransform)slider.transform, 28, y + 40, 446, 24);
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = integer;
            slider.value = value;
            UnityEventTools.AddStringPersistentListener(slider.onValueChanged, target.SendCustomEvent, "OnControlsChanged");
            return slider;
        }

        static void ConfigureMillion(FluxParticleAuthoring authoring)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("particleCount").intValue = 1048576;
            serialized.FindProperty("playOnAwake").boolValue = false;
            serialized.FindProperty("lifetime.mode").intValue = 0;
            serialized.FindProperty("lifetime.lifetime").floatValue = 10;
            serialized.FindProperty("playback.loop").boolValue = true;
            serialized.FindProperty("playback.startDelay").floatValue = 0;
            serialized.FindProperty("emission.rate").floatValue = 110000;
            serialized.FindProperty("emission.bursts").arraySize = 0;
            serialized.FindProperty("shape.radius").floatValue = 2.5f;
            serialized.FindProperty("initialVelocity.mode").intValue = 0;
            serialized.FindProperty("initialVelocity.speed").floatValue = 0.15f;
            serialized.FindProperty("render.mode").intValue = (int)FluxParticleRenderMode.Billboard;
            serialized.FindProperty("render.startSizeMode").intValue = 0;
            serialized.FindProperty("render.startSize").floatValue = 0.3f;
            serialized.FindProperty("render.startColorMode").intValue = 0;
            serialized.FindProperty("render.startColor").colorValue = new Color(0.1f, 0.7f, 1, 0.35f);
            serialized.FindProperty("render.blendMode").intValue = (int)FluxParticleBlendMode.Additive;
            serialized.FindProperty("render.intensity").floatValue = 1;
            serialized.FindProperty("render.intensityMask").objectReferenceValue = null;
            serialized.FindProperty("render.tiling").vector2Value = new Vector2(0.5f, 0.5f);
            serialized.FindProperty("render.offset").vector2Value = Vector2.zero;
            serialized.FindProperty("render.cullingBounds").boundsValue = new Bounds(Vector3.zero, Vector3.one * 40);
            var modules = serialized.FindProperty("modules");
            modules.arraySize = 2;
            modules.GetArrayElementAtIndex(0).managedReferenceValue = new FluxParticleNoiseModule();
            modules.GetArrayElementAtIndex(1).managedReferenceValue = new FluxParticleColorOverLifetimeModule();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            serialized.FindProperty("modules.Array.data[0].strength").floatValue = 0.15f;
            serialized.FindProperty("modules.Array.data[0].scale").floatValue = 0.7f;
            serialized.FindProperty("modules.Array.data[0].speed").floatValue = 0.2f;
            serialized.FindProperty("modules.Array.data[1].endColor").colorValue = new Color(1, 0.2f, 0.7f, 0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
        }

        static void Validate(Scene scene, bool synced)
        {
            var systems = Components<FluxParticleSystem>(scene);
            if (systems.Length != (synced ? 6 : 1)) throw new InvalidOperationException("Unexpected System count.");
            foreach (var system in systems)
            {
                var authoring = system.GetComponent<FluxParticleAuthoring>();
                if (authoring == null || authoring.PlayOnAwake) throw new InvalidOperationException("Startup must be controlled by the sample.");
                if (!synced && (authoring.ParticleCount != 1048576 || authoring.Modules.Count != 2))
                {
                    throw new InvalidOperationException("Million particle configuration is invalid.");
                }
                FluxParticleBuildPipeline.Build(authoring);
            }
            foreach (var proxy in Components<UdonSharpBehaviour>(scene))
            {
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(proxy);
                if (backing == null || backing.programSource == null) throw new InvalidOperationException("Missing program: " + proxy.name);
            }
            if (synced)
            {
                var shared = Components<FluxRuntimeApiSampleSync>(scene).Single();
                var local = Components<FluxRuntimeApiSample>(scene).Single();
                if (shared.gameObject == local.gameObject) throw new InvalidOperationException("Manual and None behaviours must be separate.");
                var data = new SerializedObject(local);
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(shared);
                foreach (var name in new[] { "controls", "moduleToggles" })
                {
                    var array = data.FindProperty(name);
                    for (var i = 0; i < array.arraySize; i++)
                    {
                        var control = array.GetArrayElementAtIndex(i).objectReferenceValue;
                        var changed = control is Slider slider ? (UnityEventBase)slider.onValueChanged : ((Toggle)control).onValueChanged;
                        RequireEvent(changed, backing);
                    }
                }
                RequireEvent(((Toggle)data.FindProperty("loopToggle").objectReferenceValue).onValueChanged, backing);
                RequireEvent(((Dropdown)data.FindProperty("shapeSelector").objectReferenceValue).onValueChanged, backing);
                var values = local.CaptureValues();
                var modules = local.CaptureModules();
                if (values.Length != (int)SampleControl.Count || modules.Length != (int)SampleModule.Count)
                {
                    throw new InvalidOperationException("Incomplete shared snapshot.");
                }
                var previous = values[0];
                values[0] = -1;
                if (local.CaptureValues()[0] != previous) throw new InvalidOperationException("Snapshot aliases the UI state.");
                if (!data.FindProperty("startPaused").boolValue) throw new InvalidOperationException("Shared sample must wait for network state.");
            }
            else
            {
                var controller = Components<FluxMillionParticlesSample>(scene).Single();
                var data = new SerializedObject(controller);
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
                foreach (var name in new[] { "rate", "noise", "intensity" })
                {
                    RequireEvent(((Slider)data.FindProperty(name).objectReferenceValue).onValueChanged, backing);
                    if (data.FindProperty(name + "Label").objectReferenceValue == null)
                    {
                        throw new InvalidOperationException("Missing slider value label: " + name);
                    }
                }
                if (Components<FluxRuntimeApiSample>(scene).Length != 0) throw new InvalidOperationException("Full UI controller remains in the million sample.");
            }
        }

        static void RequireEvent(UnityEventBase changed, UdonBehaviour target)
        {
            if (changed.GetPersistentEventCount() != 1 || changed.GetPersistentTarget(0) != target ||
                changed.GetPersistentMethodName(0) != nameof(UdonBehaviour.SendCustomEvent))
            {
                throw new InvalidOperationException("Sample UI event has an invalid target.");
            }
        }
    }
}
