using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 生成物の明示的な分析・整理と、任意の自動整理を提供します。
    /// </summary>
    [InitializeOnLoad]
    public static class FluxParticleGenerationCleanup
    {
        const string AutomaticCleanupKey = "TpLab.Flux.FX.AutomaticGenerationCleanup";
        const string AutomaticCleanupMenu = "Tools/FluxFX/Automatic Generated Asset Cleanup";
        static bool _quitting;
        static FluxParticleGenerationCleanupSchedule _schedule;

        /// <summary>
        /// 自動整理の有効状態。既定ONで、既存のEditor設定を維持します。
        /// </summary>
        public static bool AutomaticCleanupEnabled
        {
            get => EditorPrefs.GetBool(AutomaticCleanupKey, true);
            set
            {
                EditorPrefs.SetBool(AutomaticCleanupKey, value);
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
        /// 次回起動時の再確認を予約します。このEditor実行中には自動走査しません。
        /// </summary>
        public static void Request()
        {
            var schedule = GetSchedule();
            if (schedule.Pending) return;
            schedule.completedRevision++;
            schedule.Save();
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
        }

        internal static void FullScanStarted(string root)
        {
            var schedule = GetSchedule();
            schedule.lastFullScanUtc = DateTime.UtcNow.ToString("O");
            schedule.Save();
        }

        // 起動の機会判定はStartup側で一度だけ行い、ここでは永続的な頻度制限と結果を扱います。
        internal static FluxParticleGenerationCleanupReport CollectAtStartup(string root, Func<string, float, bool> progress)
        {
            var schedule = GetSchedule();
            if (!AutomaticCleanupEnabled || !schedule.IsDue(DateTime.UtcNow)) return null;
            var revision = schedule.completedRevision;
            var report = FluxParticleGenerationCleanupAnalysis.Run(root, DateTime.UtcNow.AddDays(-1), true, null, progress, () =>
            {
                // 途中失敗やキャンセルでも、開始した全体走査を24時間以内に繰り返しません。
                schedule.lastAutomaticScanUtc = DateTime.UtcNow.ToString("O");
                schedule.Save();
            });
            if (!report.blocked && !report.failed && report.items.All(item => item.status != "Failed"))
            {
                schedule.scannedRevision = revision;
                schedule.nextCandidateCheckUtc = NextRetentionCheck(root, DateTime.UtcNow);
                schedule.Save();
            }
            return report;
        }

        static FluxParticleGenerationCleanup()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            EditorSceneManager.sceneSaved += _ => OnProjectChanged();
            EditorSceneManager.sceneClosed += _ => OnProjectChanged();
            EditorApplication.quitting += () => _quitting = true;
        }

        static FluxParticleGenerationCleanupSchedule GetSchedule()
        {
            return _schedule ?? (_schedule = FluxParticleGenerationCleanupSchedule.Load());
        }

        static void OnProjectChanged()
        {
            // 記録があるプロジェクトだけ予約します。イベント内でAsset全体を走査しません。
            if (!File.Exists(FluxParticleGenerationLedger.PathFor(FluxParticleGenerationSession.DefaultRoot))) return;
            try { Request(); }
            catch (Exception error)
            {
                Debug.LogWarning("[FluxFX] Could not schedule generated asset cleanup: " + error.Message);
            }
        }

        static string NextRetentionCheck(string root, DateTime now)
        {
            var next = DateTime.MaxValue;
            foreach (var asset in FluxParticleGenerationLedger.Load(root).assets)
            {
                var until = asset.kind == "Material" ? DateTime.MinValue :
                    FluxParticleGenerationLedger.ParseUtc(asset.lastUsedUtc);
                if (until != DateTime.MinValue && until < DateTime.MaxValue.AddDays(-1))
                {
                    until = until.AddDays(1);
                }
                if (!string.IsNullOrEmpty(asset.retainUntilUtc))
                {
                    var failureUntil = FluxParticleGenerationLedger.ParseUtc(asset.retainUntilUtc);
                    if (failureUntil > until)
                    {
                        until = failureUntil;
                    }
                }
                if (until > now && until < next)
                {
                    next = until;
                }
            }
            return next == DateTime.MaxValue ? null : next.ToString("O");
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
