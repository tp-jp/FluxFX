using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// PlayとBuildの実行全体に対応する生成セッションを管理します。
    /// </summary>
    [InitializeOnLoad]
    public static class FluxParticleGenerationLifecycle
    {
        const string PlayKey = "FluxFX.Generation.Play";
        const string BuildKey = "FluxFX.Generation.Build";
        const string SdkKey = "FluxFX.Generation.SDK";
        static IVRCSdkBuilderApi sdk;
        static double nextCheck;

        static FluxParticleGenerationLifecycle()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Update;
            EditorApplication.quitting += OnQuit;
            AssemblyReloadEvents.beforeAssemblyReload += DetachSdk;
            VRCSdkControlPanel.OnSdkPanelEnable += OnSdkPanelEnable;
            EditorApplication.delayCall += Recover;
        }

        /// <summary>
        /// 現在の実行に対応するセッション
        /// </summary>
        public static FluxParticleGenerationSession Current => Read(SdkKey) ?? Read(BuildKey) ?? Read(PlayKey);

        public static void BeginPlayerBuild()
        {
            Begin(BuildKey, "Build");
        }

        public static void EndPlayerBuild(bool succeeded)
        {
            var session = Read(BuildKey);
            if (session != null && !succeeded)
            {
                session.RecordFailure("Unity build did not succeed.");
            }
            End(BuildKey);
        }

        static void Begin(string key, string purpose)
        {
            if (Read(key) != null) return;
            SessionState.SetString(key, FluxParticleGenerationSession.Begin(purpose).Id);
        }

        static FluxParticleGenerationSession Read(string key)
        {
            var id = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                var session = FluxParticleGenerationSession.Load(id);
                return session.State == "Running" ? session : null;
            }
            catch (Exception error)
            {
                SessionState.EraseString(key);
                Debug.LogWarning("[FluxFX] Generation session could not be restored: " + id + "\n" + error.Message);
                return null;
            }
        }

        static void End(string key)
        {
            Read(key)?.Dispose();
            SessionState.EraseString(key);
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Begin(PlayKey, "Play");
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                End(PlayKey);
            }
        }

        static void Recover()
        {
            var active = new[] { PlayKey, BuildKey, SdkKey }.Select(key => SessionState.GetString(key, "")).ToArray();
            if (!Directory.Exists(FluxParticleGenerationSession.ManifestRoot)) return;
            foreach (var path in Directory.GetFiles(FluxParticleGenerationSession.ManifestRoot, "*.json"))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                if (active.Contains(id)) continue;
                try
                {
                    var session = FluxParticleGenerationSession.Load(id);
                    if (session.EditorSessionId != FluxParticleGenerationSession.CurrentEditorSessionId)
                    {
                        session.Interrupt();
                    }
                }
                catch (Exception error)
                {
                    Debug.LogWarning("[FluxFX] Generation manifest could not be recovered: " + path + "\n" + error.Message);
                }
            }
        }

        static void Update()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (Resources.FindObjectsOfTypeAll<VRCSdkControlPanel>().Length > 0 &&
                VRCSdkControlPanel.TryGetBuilder<IVRCSdkBuilderApi>(out var builder) && !ReferenceEquals(builder, sdk))
            {
                DetachSdk();
                sdk = builder;
                sdk.OnSdkBuildStart += OnSdkStart;
                sdk.OnSdkBuildError += OnSdkError;
                sdk.OnSdkBuildFinish += OnSdkFinish;
            }
            if (!BuildPipeline.isBuildingPlayer && Read(BuildKey) != null)
            {
                EndPlayerBuild(false);
            }
        }

        static void OnSdkPanelEnable(object sender, EventArgs args)
        {
            nextCheck = 0;
            EditorApplication.delayCall += Update;
        }

        static void OnSdkStart(object sender, object args) => Begin(SdkKey, "SDKBuild");

        static void OnSdkError(object sender, string error)
        {
            Read(SdkKey)?.RecordFailure(error);
        }

        static void OnSdkFinish(object sender, string message) => End(SdkKey);

        static void DetachSdk()
        {
            if (sdk == null) return;
            sdk.OnSdkBuildStart -= OnSdkStart;
            sdk.OnSdkBuildError -= OnSdkError;
            sdk.OnSdkBuildFinish -= OnSdkFinish;
            sdk = null;
        }

        static void OnQuit()
        {
            foreach (var key in new[] { PlayKey, BuildKey, SdkKey })
            {
                Read(key)?.Interrupt();
                SessionState.EraseString(key);
            }
            DetachSdk();
        }
    }
}
