using System;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 生成物の明示的な分析・整理と、任意の自動整理を提供します。
    /// </summary>
    [InitializeOnLoad]
    public static class FluxParticleGenerationCleanup
    {
        const double QuietSeconds = 60;
        const string AutomaticCleanupKey = "TpLab.Flux.FX.AutomaticGenerationCleanup";
        const string AutomaticCleanupMenu = "Tools/FluxFX/Automatic Generated Asset Cleanup";
        static double _nextAttempt;
        static bool _quitting;
        static bool _scheduleError;
        static FluxParticleGenerationCleanupSchedule _schedule;

        /// <summary>
        /// 自動整理の有効状態。既定OFFで、既存のEditor設定を維持します。
        /// </summary>
        public static bool AutomaticCleanupEnabled
        {
            get => EditorPrefs.GetBool(AutomaticCleanupKey, false);
            set
            {
                EditorPrefs.SetBool(AutomaticCleanupKey, value);
                _nextAttempt = EditorApplication.timeSinceStartup + QuietSeconds;
                _scheduleError = false;
                _schedule = null;
            }
        }

        /// <summary>
        /// 指定ルートを分析します。Assetは削除しません。旧管理記録の台帳への移行は行います。
        /// </summary>
        /// <param name="root">Beginで登録した生成ルート</param>
        /// <returns>削除候補、保持理由、参照元と診断</returns>
        public static FluxParticleGenerationCleanupReport Analyze(string root = FluxParticleGenerationSession.DefaultRoot)
        {
            return FluxParticleGenerationCleanupAnalysis.Run(root, null, false);
        }

        /// <summary>
        /// 最新の参照状態を再分析し、未使用の管理対象だけを依存順に削除します。
        /// </summary>
        /// <param name="root">Beginで登録した生成ルート</param>
        /// <returns>削除・保持・失敗の結果。診断JSONはTempへ保存</returns>
        public static FluxParticleGenerationCleanupReport CollectWithReport(string root = FluxParticleGenerationSession.DefaultRoot)
        {
            return FluxParticleGenerationCleanupAnalysis.Run(root, null, true);
        }

        /// <summary>
        /// 表示した候補だけを、最新の参照とGUIDを再確認して整理します。新たな候補は削除しません。
        /// </summary>
        /// <param name="review">Analyzeが返した確認対象</param>
        /// <returns>再分析後の整理結果</returns>
        public static FluxParticleGenerationCleanupReport CollectReviewed(FluxParticleGenerationCleanupReport review)
        {
            if (review == null) throw new ArgumentNullException(nameof(review));
            return FluxParticleGenerationCleanupAnalysis.Run(review.root, null, true, review);
        }

        /// <summary>
        /// 指定ルートの未使用生成物を整理します。管理記録を読めない場合は削除せず例外を通知します。
        /// </summary>
        /// <param name="root">Beginで登録した生成ルート</param>
        /// <returns>削除したAsset数</returns>
        public static int Collect(string root = FluxParticleGenerationSession.DefaultRoot)
        {
            return CollectCore(root, null);
        }

        /// <summary>
        /// 未走査の生成完了記録がある場合に限り、待機時間を設定します。生成変更や走査は行いません。
        /// </summary>
        public static void Request()
        {
            if (!AutomaticCleanupEnabled || _quitting) return;
            _nextAttempt = EditorApplication.timeSinceStartup + QuietSeconds;
        }

        internal static bool IsBusy => _quitting || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer ||
            FluxParticleGenerationLifecycle.Current != null;

        internal static void NotifyCompleted()
        {
            var schedule = GetSchedule();
            schedule.completedRevision++;
            schedule.nextCandidateCheckUtc = null;
            schedule.Save();
            Request();
        }

        internal static void FullScanStarted(string root)
        {
            _scheduleError = false;
            var schedule = GetSchedule();
            schedule.lastFullScanUtc = DateTime.UtcNow.ToString("O");
            if (root == FluxParticleGenerationSession.DefaultRoot)
            {
                schedule.scannedRevision = schedule.completedRevision;
            }
            // 走査が途中で失敗した場合も、繰り返し自動走査しません。
            schedule.Save();
        }

        static FluxParticleGenerationCleanup()
        {
            EditorApplication.update += Update;
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.quitting += () => _quitting = true;
            _nextAttempt = EditorApplication.timeSinceStartup + QuietSeconds;
        }

        static FluxParticleGenerationCleanupSchedule GetSchedule()
        {
            return _schedule ?? (_schedule = FluxParticleGenerationCleanupSchedule.Load());
        }

        static void OnProjectChanged()
        {
            // Scene保存やAsset編集は走査を予約せず、処理中のImportとの競合だけを避けます。
            if (AutomaticCleanupEnabled)
            {
                _nextAttempt = EditorApplication.timeSinceStartup + QuietSeconds;
            }
        }

        static void Update()
        {
            if (!AutomaticCleanupEnabled || _scheduleError || _quitting) return;
            var now = EditorApplication.timeSinceStartup;
            if (IsBusy)
            {
                _nextAttempt = now + QuietSeconds;
                return;
            }
            if (now < _nextAttempt) return;
            try
            {
                if (!GetSchedule().IsDue(DateTime.UtcNow)) return;
                _nextAttempt = now + QuietSeconds;
                var report = FluxParticleGenerationCleanupAnalysis.Run(FluxParticleGenerationSession.DefaultRoot,
                    DateTime.UtcNow.AddDays(-1), true);
                if (!report.fullScan && !report.blocked)
                {
                    // 保持期間内のCacheしかない場合も、履歴の読み直しを毎分繰り返しません。
                    var schedule = GetSchedule();
                    schedule.nextCandidateCheckUtc = DateTime.UtcNow.AddDays(1).ToString("O");
                    schedule.Save();
                }
                if (report.DeletedCount > 0)
                {
                    Debug.Log("[FluxFX] Removed unused generated assets: " + report.DeletedCount + "\n" + report.logPath);
                }
                if (report.failed)
                {
                    _scheduleError = true;
                    Debug.LogWarning("[FluxFX] Automatic cleanup is deferred. Open Clean Unused Generated Assets for diagnostics.\n" +
                        string.Join("\n", report.diagnostics));
                }
            }
            catch (Exception error)
            {
                _scheduleError = true;
                Debug.LogWarning("[FluxFX] Automatic cleanup is deferred: " + error.Message);
            }
        }

        static int CollectCore(string root, DateTime? cacheCutoff)
        {
            var report = FluxParticleGenerationCleanupAnalysis.Run(root, cacheCutoff, true);
            if (report.failed)
                throw new InvalidOperationException(string.Join("\n", report.diagnostics));
            return report.DeletedCount;
        }

        [MenuItem("Tools/FluxFX/Clean Unused Generated Assets")]
        static void CleanFromMenu() => FluxParticleGenerationCleanupWindow.Open();

        [MenuItem(AutomaticCleanupMenu)]
        static void ToggleAutomaticCleanup() => AutomaticCleanupEnabled = !AutomaticCleanupEnabled;

        [MenuItem(AutomaticCleanupMenu, true)]
        static bool ValidateAutomaticCleanup()
        {
            Menu.SetChecked(AutomaticCleanupMenu, AutomaticCleanupEnabled);
            return true;
        }
    }
}
