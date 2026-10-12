using System;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    [InitializeOnLoad]
    public static class ShaderArtifactAutoCleanup
    {
        const string EnabledKey = "FluxFX.ShaderArtifactAutoCleanup.Enabled";
        const string PendingKey = "FluxFX.ShaderArtifactAutoCleanup.Pending";
        const double DelaySeconds = 3.0;

        static bool _pending;
        static double _requestedAt;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        static ShaderArtifactAutoCleanup()
        {
            _pending = SessionState.GetBool(PendingKey, false);
            _requestedAt = EditorApplication.timeSinceStartup;

            EditorApplication.update += Update;
        }

        public static void RequestAfterSuccessfulBuild()
        {
            if (!Enabled) return;

            _pending = true;
            _requestedAt = EditorApplication.timeSinceStartup;

            SessionState.SetBool(PendingKey, true);
        }

        static void Update()
        {
            if (!_pending) return;
            if (EditorApplication.timeSinceStartup - _requestedAt < DelaySeconds) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (BuildPipeline.isBuildingPlayer) return;

            // 失敗時に自動で繰り返さないよう、実行前に要求を消費する。
            _pending = false;
            SessionState.SetBool(PendingKey, false);

            try
            {
                new ShaderArtifactAutoCleanupExecutor().Execute();
            }
            catch (Exception exception)
            {
                Logger.LogError($"[FluxFX] Automatic Shader Cleanup failed: {exception}");
            }
        }
    }
}