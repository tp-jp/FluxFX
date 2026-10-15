using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Samples.Benchmark.Editor
{
    /// <summary>
    /// 同じSceneの複製で、Trailsの未組み込み・OFF・ONを順に計測します。
    /// </summary>
    [InitializeOnLoad]
    public static class FluxTrailBenchmarkRunner
    {
        const string SourceScene = "Assets/FluxFX/Samples/Benchmark/GPUBenchmarkScene.unity";
        const string SessionKey = "FluxFX.TrailBenchmark";
        const float WarmupSeconds = 20;
        const float MeasureSeconds = 10;
        static readonly string[] Cases = { "Absent", "Off", "On" };
        static readonly FrameTiming[] Timings = new FrameTiming[1];
        static readonly List<double> Frames = new List<double>();
        static readonly List<double> Cpu = new List<double>();
        static readonly List<double> Gpu = new List<double>();
        static readonly List<string> Rows = new List<string>();
        static RunState state;
        static float elapsed;
        static bool measuring;
        static bool needsWarmup;
        static int lastFrame = -1;
        static ulong lastTimestamp;
        static int settleFrames;
        static int invalidGpu;
        static int duplicateTimings;
        static int focusInterruptions;
        static int originalVsync;
        static int originalFrameRate;
        static bool overridesApplied;
        static bool handlingError;

        [Serializable]
        sealed class SavedScene
        {
            public string path;
            public bool loaded;
            public bool active;
        }

        [Serializable]
        sealed class RunState
        {
            public string folder;
            public string output;
            public SavedScene[] scenes;
            public int caseIndex;
            public int width;
            public int height;
            public string phase;
        }

        static FluxTrailBenchmarkRunner()
        {
            var saved = SessionState.GetString(SessionKey, "");
            if (!string.IsNullOrEmpty(saved)) state = JsonUtility.FromJson<RunState>(saved);
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayMode;
            Application.logMessageReceived += OnLog;
            EditorApplication.delayCall += Resume;
        }

        [MenuItem("Tools/FluxFX/Benchmark/Compare Trails")]
        public static void Run()
        {
            if (state != null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scenes = EditorSceneManager.GetSceneManagerSetup();
            if (scenes.Any(scene => string.IsNullOrEmpty(scene.path)) ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(index => SceneManager.GetSceneAt(index).isDirty))
            {
                Debug.LogWarning("[FluxFX Benchmark] Save open scenes before starting the comparison.");
                return;
            }
            var id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            state = new RunState
            {
                folder = "Assets/FluxFX/Samples/Benchmark/Run-" + id,
                output = "Temp/FluxFXBenchmarks/Trails-" + id,
                scenes = scenes.Select(scene => new SavedScene { path = scene.path, loaded = scene.isLoaded, active = scene.isActive }).ToArray(),
                phase = "Prepare"
            };
            Directory.CreateDirectory(state.output);
            AssetDatabase.CreateFolder("Assets/FluxFX/Samples/Benchmark", "Run-" + id);
            Save();
            Prepare();
        }

        [MenuItem("Tools/FluxFX/Benchmark/Cancel Trail Comparison")]
        public static void Cancel()
        {
            if (state == null) return;
            File.AppendAllText(state.output + "/progress.txt", "Cancelled\n");
            state.phase = "Cancel";
            Save();
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else Finish();
        }

        static void Save()
        {
            SessionState.SetString(SessionKey, JsonUtility.ToJson(state));
        }

        static void Resume()
        {
            if (state == null) return;
            if (EditorApplication.isPlaying && state.phase == "Measure") StartCase();
            else if (!EditorApplication.isPlayingOrWillChangePlaymode && state.phase == "Prepare") Prepare();
            else if (!EditorApplication.isPlayingOrWillChangePlaymode && state.phase == "Cancel") Finish();
        }

        static void Prepare()
        {
            if (state == null || state.phase != "Prepare") return;
            try
            {
                var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
                var scenePath = state.folder + "/" + Cases[state.caseIndex] + ".unity";
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new InvalidOperationException("Could not copy the benchmark scene.");
                var systems = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FluxParticleSystem>(true)).ToArray();
                if (systems.Length != 1 || systems[0].ParticleCount != 16384)
                {
                    throw new InvalidOperationException("The source benchmark must contain one System with 16384 slots.");
                }
                var authoring = systems[0].GetComponent<FluxParticleAuthoring>();
                if (authoring == null) throw new InvalidOperationException("Benchmark Authoring is missing.");
                var modules = authoring.Modules.Where(module => !(module is FluxParticleTrailsModule)).ToList();
                if (state.caseIndex != 0)
                {
                    var trails = new FluxParticleTrailsModule();
                    typeof(FluxParticleModule).GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(trails, state.caseIndex == 2);
                    modules.Add(trails);
                }
                typeof(FluxParticleAuthoring).GetField("modules", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(authoring, modules);
                EditorUtility.SetDirty(authoring);
                PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
                foreach (var benchmark in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FluxFXGpuBenchmark>(true)))
                {
                    Object.DestroyImmediate(benchmark);
                }
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new InvalidOperationException("Could not save the temporary benchmark scene.");
                File.AppendAllText(state.output + "/progress.txt", Cases[state.caseIndex] + ": scene prepared\n");
                var description = "ParticleCount=" + systems[0].ParticleCount + "\nRate=" + authoring.Emission.Rate +
                    "\nLifetime=" + authoring.Lifetime.Lifetime + "\nModules=" + string.Join(",", modules.Select(module => module.GetType().Name + ":" + module.Enabled)) +
                    "\nUnity=" + Application.unityVersion + "\nGPU=" + SystemInfo.graphicsDeviceName +
                    "\nGraphicsAPI=" + SystemInfo.graphicsDeviceType + "\nCPU=" + SystemInfo.processorType +
                    "\nWarmupSeconds=" + WarmupSeconds + "\nMeasureSeconds=" + MeasureSeconds +
                    "\nExecution=Unity Editor Play Mode / Udon VM\n";
                File.WriteAllText(state.output + "/" + Cases[state.caseIndex] + "-settings.txt", description);
                state.phase = "Measure";
                Save();
                EditorApplication.isPlaying = true;
            }
            catch (Exception error)
            {
                Fail(error.ToString());
            }
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (state == null) return;
            if (change == PlayModeStateChange.EnteredPlayMode) StartCase();
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                RestoreLimits();
                if (state.phase == "Measure")
                {
                    state.phase = "Cancel";
                    Save();
                    File.AppendAllText(state.output + "/progress.txt", "Play stopped before measurement completed.\n");
                }
            }
            if (change != PlayModeStateChange.EnteredEditMode) return;
            if (state.phase == "Next")
            {
                state.caseIndex++;
                state.phase = state.caseIndex < Cases.Length ? "Prepare" : "Cancel";
                Save();
                EditorApplication.delayCall += state.phase == "Prepare" ? (EditorApplication.CallbackFunction)Prepare : Finish;
            }
            else if (state.phase == "Cancel") EditorApplication.delayCall += Finish;
        }

        static void StartCase()
        {
            if (state == null || state.phase != "Measure") return;
            Frames.Clear(); Cpu.Clear(); Gpu.Clear(); Rows.Clear();
            elapsed = 0;
            measuring = false;
            needsWarmup = false;
            lastFrame = -1;
            lastTimestamp = 0;
            invalidGpu = 0;
            duplicateTimings = 0;
            focusInterruptions = 0;
            if (!overridesApplied)
            {
                originalVsync = QualitySettings.vSyncCount;
                originalFrameRate = Application.targetFrameRate;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                overridesApplied = true;
            }
            Debug.Log("[FluxFX Benchmark] " + Cases[state.caseIndex] + ": warmup 20s, measurement 10s. Keep Unity focused and Game View unchanged.");
        }

        static void RestoreLimits()
        {
            if (!overridesApplied) return;
            QualitySettings.vSyncCount = originalVsync;
            Application.targetFrameRate = originalFrameRate;
            overridesApplied = false;
        }

        static void Update()
        {
            if (state == null || state.phase != "Measure" || !EditorApplication.isPlaying) return;
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                if (!needsWarmup) focusInterruptions++;
                needsWarmup = true;
                return;
            }
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            if (needsWarmup)
            {
                Frames.Clear(); Cpu.Clear(); Gpu.Clear(); Rows.Clear();
                elapsed = 0;
                measuring = false;
                needsWarmup = false;
                lastTimestamp = 0;
                invalidGpu = 0;
                duplicateTimings = 0;
            }
            var gameViewSize = Handles.GetMainGameViewSize();
            var width = Mathf.RoundToInt(gameViewSize.x);
            var height = Mathf.RoundToInt(gameViewSize.y);
            elapsed += Time.unscaledDeltaTime;
            if (!measuring)
            {
                FrameTimingManager.CaptureFrameTimings();
                if (elapsed < WarmupSeconds) return;
                var executor = Object.FindObjectOfType<FluxParticleExecutor>();
                var backing = executor == null ? null : UdonSharpEditorUtility.GetBackingUdonBehaviour(executor);
                if (backing == null || !(backing.GetProgramVariable("_initialized") is bool initialized) || !initialized)
                {
                    Fail("Executor is not initialized on the Udon VM; timing this scene would not measure the intended workload.");
                    return;
                }
                var moduleIds = backing.GetProgramVariable("moduleIds") as string[];
                var ids = backing.GetProgramVariable("parameterIds") as string[];
                var values = backing.GetProgramVariable("parameterValues") as Vector4[];
                var hasTrails = moduleIds != null && moduleIds.Contains("tplab.fluxfx.trails");
                var slot = ids == null ? -1 : Array.IndexOf(ids, "trails.enabled");
                if ((int)backing.GetProgramVariable("_count") != 16384 || hasTrails != (state.caseIndex != 0) ||
                    hasTrails && (slot < 0 || values == null || (values[slot].x > 0.5f) != (state.caseIndex == 2)))
                {
                    Fail("The Udon VM workload does not match the selected benchmark case.");
                    return;
                }
                if (state.width == 0)
                {
                    state.width = width;
                    state.height = height;
                    Save();
                }
                if (state.width != width || state.height != height)
                {
                    Fail("Game View resolution changed between cases: " + state.width + "x" + state.height + " -> " + width + "x" + height);
                    return;
                }
                measuring = true;
                elapsed = 0;
                settleFrames = 8;
                File.AppendAllText(state.output + "/progress.txt", Cases[state.caseIndex] + ": measurement started, " + state.width + "x" + state.height + "\n");
                return;
            }
            if (width != state.width || height != state.height)
            {
                Fail("Game View resolution changed during measurement: " + state.width + "x" + state.height + " -> " + width + "x" + height);
                return;
            }
            if (elapsed >= MeasureSeconds)
            {
                CompleteCase();
                return;
            }
            FrameTimingManager.CaptureFrameTimings();
            if (settleFrames-- > 0) return;
            var frame = Time.unscaledDeltaTime * 1000.0;
            var cpu = double.NaN;
            var gpu = double.NaN;
            var count = FrameTimingManager.GetLatestTimings(1, Timings);
            if (count != 0 && Timings[0].frameStartTimestamp != 0 && Timings[0].frameStartTimestamp > lastTimestamp)
            {
                lastTimestamp = Timings[0].frameStartTimestamp;
                if (Valid(Timings[0].cpuFrameTime)) cpu = Timings[0].cpuFrameTime;
                if (Valid(Timings[0].gpuFrameTime)) gpu = Timings[0].gpuFrameTime;
            }
            else duplicateTimings++;
            if (Valid(frame)) Frames.Add(frame);
            if (Valid(cpu)) Cpu.Add(cpu);
            if (Valid(gpu)) Gpu.Add(gpu);
            else invalidGpu++;
            Rows.Add(Format(elapsed) + "," + Format(frame) + "," + Format(cpu) + "," + Format(gpu));
        }

        static void CompleteCase()
        {
            var name = Cases[state.caseIndex];
            var output = state.output;
            File.WriteAllLines(output + "/" + name + "-samples.csv", new[] { "Seconds,FrameMs,CpuFrameMs,GpuFrameMs" }.Concat(Rows));
            var summary = output + "/summary.csv";
            if (!File.Exists(summary))
            {
                File.WriteAllText(summary, "Case,Width,Height,Frames,CpuSamples,GpuSamples,InvalidGpu,DuplicateTimings,FocusInterruptions,FrameMeanMs,FrameMedianMs,FrameP95Ms,CpuMedianMs,GpuMedianMs,GpuP95Ms\n");
            }
            File.AppendAllText(summary, name + "," + state.width + "," + state.height + "," + Frames.Count + "," + Cpu.Count + "," + Gpu.Count +
                "," + invalidGpu + "," + duplicateTimings + "," + focusInterruptions + "," +
                Format(Frames.Count == 0 ? double.NaN : Frames.Average()) + "," + Format(Percentile(Frames, 0.5)) + "," + Format(Percentile(Frames, 0.95)) +
                "," + Format(Percentile(Cpu, 0.5)) + "," + Format(Percentile(Gpu, 0.5)) + "," + Format(Percentile(Gpu, 0.95)) + "\n");
            File.AppendAllText(output + "/progress.txt", name + ": complete; GPU samples=" + Gpu.Count + ", frame samples=" + Frames.Count + "\n");
            state.phase = "Next";
            Save();
            EditorApplication.isPlaying = false;
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (state == null || state.phase != "Measure" || handlingError || type != LogType.Error && type != LogType.Exception) return;
            Fail(message + "\n" + stack);
        }

        static void Fail(string message)
        {
            if (state == null || handlingError) return;
            handlingError = true;
            File.AppendAllText(state.output + "/progress.txt", "FAILED: " + message + "\n");
            Debug.LogWarning("[FluxFX Benchmark] Comparison stopped: " + message);
            state.phase = "Cancel";
            Save();
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else EditorApplication.delayCall += Finish;
            handlingError = false;
        }

        static void Finish()
        {
            if (state == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var finished = state;
            state = null;
            SessionState.EraseString(SessionKey);
            RestoreLimits();
            EditorSceneManager.RestoreSceneManagerSetup(finished.scenes.Select(scene => new SceneSetup
            {
                path = scene.path, isLoaded = scene.loaded, isActive = scene.active
            }).ToArray());
            AssetDatabase.DeleteAsset(finished.folder);
            Debug.Log("[FluxFX Benchmark] Comparison ended. Results: " + finished.output);
        }

        static bool Valid(double value) => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);

        static string Format(double value) => double.IsNaN(value) ? "" : value.ToString("F4", CultureInfo.InvariantCulture);

        static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0) return double.NaN;
            var sorted = values.OrderBy(value => value).ToArray();
            var position = (sorted.Length - 1) * percentile;
            var first = (int)Math.Floor(position);
            var last = (int)Math.Ceiling(position);
            return sorted[first] + (sorted[last] - sorted[first]) * (position - first);
        }
    }
}
