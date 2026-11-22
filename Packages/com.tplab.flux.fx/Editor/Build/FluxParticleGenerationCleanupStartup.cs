using System;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Build
{
    // SessionStateはプロジェクトを閉じるまで保持され、スクリプト再コンパイルでは消えません。
    [InitializeOnLoad]
    internal static class FluxParticleGenerationCleanupStartup
    {
        internal const string StateKey = "FluxFX.Generation.CleanupStartup";
        const int Waiting = 1;
        const int Attempted = 2;
        const int Skipped = 3;
        static bool _editorReady;

        static FluxParticleGenerationCleanupStartup()
        {
            Initialize(StateKey, !string.IsNullOrEmpty(SessionState.GetString("FluxFX.Generation.EditorInstance", "")));
            if (SessionState.GetInt(StateKey, 0) != Waiting) return;
            EditorApplication.delayCall += () => _editorReady = true;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        internal static void Initialize(string key, bool alreadyOpen)
        {
            if (SessionState.GetInt(key, 0) != 0) return;
            // 既にFluxFXを使用しているEditorへの更新導入を、新しいプロジェクト起動と扱いません。
            SessionState.SetInt(key, alreadyOpen ? Skipped : Waiting);
        }

        internal static bool TryEnter(string key, bool ready, bool importing, bool interrupted)
        {
            if (SessionState.GetInt(key, 0) != Waiting) return false;
            if (interrupted)
            {
                SessionState.SetInt(key, Skipped);
                return false;
            }
            if (!ready || importing) return false;
            // 設定OFF、24時間以内、候補なしの場合も、この起動での機会はここで終了します。
            SessionState.SetInt(key, Attempted);
            return true;
        }

        internal static void Skip()
        {
            if (SessionState.GetInt(StateKey, 0) == Waiting)
            {
                SessionState.SetInt(StateKey, Skipped);
            }
            Detach();
        }

        static void Update()
        {
            var entered = TryEnter(StateKey, _editorReady && FluxParticleGenerationLifecycle.RecoveryComplete,
                EditorApplication.isCompiling || EditorApplication.isUpdating,
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer ||
                FluxParticleGenerationLifecycle.Current != null || Application.isBatchMode);
            if (SessionState.GetInt(StateKey, 0) != Waiting)
            {
                Detach();
            }
            if (!entered) return;
            try
            {
                var report = FluxParticleGenerationCleanup.CollectAtStartup(FluxParticleGenerationSession.DefaultRoot,
                    (message, progress) => EditorUtility.DisplayCancelableProgressBar(L10n.Tr("FluxFX Generated Asset Cleanup"),
                        L10n.Tr(message), progress));
                if (report == null) return;
                if (report.failed)
                {
                    Debug.LogWarning("[FluxFX] Startup cleanup deferred.\n" + string.Join("\n", report.diagnostics) + "\n" + report.logPath);
                }
                else if (report.DeletedCount > 0)
                {
                    Debug.Log("[FluxFX] Removed unused generated assets: " + report.DeletedCount + "\n" + report.logPath);
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("[FluxFX] Startup cleanup deferred: " + error.Message);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) Skip();
        }

        static void Detach()
        {
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
    }
}
