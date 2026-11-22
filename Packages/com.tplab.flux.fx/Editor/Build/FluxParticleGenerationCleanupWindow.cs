using System;
using System.Linq;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 削除予定と保持理由を確認してから、生成物を整理する画面です。
    /// </summary>
    public sealed class FluxParticleGenerationCleanupWindow : EditorWindow
    {
        [SerializeField]
        string root = FluxParticleGenerationSession.DefaultRoot;

        Vector2 _scroll;
        FluxParticleGenerationCleanupReport _report;
        string _message;

        /// <summary>
        /// 生成物の整理画面を開きます。分析や削除はボタン操作で開始します。
        /// </summary>
        public static void Open()
        {
            var window = GetWindow<FluxParticleGenerationCleanupWindow>();
            window.titleContent = new GUIContent(L10n.Tr("Generated Asset Cleanup"));
            window.minSize = new Vector2(650, 350);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(L10n.Tr("Analyze generated assets to see what can be deleted and why other assets are kept. References are checked again before deletion."), MessageType.Info);
            var nextRoot = EditorGUILayout.TextField(L10n.Tr("Generation Root"), root);
            if (nextRoot != root)
            {
                root = nextRoot;
                _report = null;
                _message = null;
            }
            var automatic = EditorGUILayout.Toggle(L10n.Tr("Automatic Cleanup"), FluxParticleGenerationCleanup.AutomaticCleanupEnabled);
            if (automatic != FluxParticleGenerationCleanup.AutomaticCleanupEnabled)
            {
                FluxParticleGenerationCleanup.AutomaticCleanupEnabled = automatic;
            }
            EditorGUILayout.LabelField(L10n.Tr("Automatic cleanup is ON by default. Pending cleanup runs only when opening the project, after initial import and compilation, at most once per 24 hours. No delayed cleanup runs while editing."), EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(FluxParticleGenerationCleanup.IsBusy))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(L10n.Tr("Analyze Generated Assets")))
                {
                    Execute(false);
                }
                using (new EditorGUI.DisabledScope(_report == null || _report.blocked || _report.CandidateCount == 0))
                {
                    if (GUILayout.Button(L10n.Tr("Delete Reviewed Unused Assets")))
                    {
                        Execute(true);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, MessageType.Error);
            }
            if (_report == null) return;
            EditorGUILayout.LabelField(L10n.Tr("Candidates"), _report.CandidateCount + " / " + EditorUtility.FormatBytes(_report.CandidateBytes));
            EditorGUILayout.LabelField(L10n.Tr("Deleted"), _report.DeletedCount.ToString());
            EditorGUILayout.LabelField(L10n.Tr("Kept"), _report.items.Count(item => item.status == "Keep").ToString());
            foreach (var diagnostic in _report.diagnostics) EditorGUILayout.HelpBox(diagnostic, _report.failed ? MessageType.Error : MessageType.Warning);
            if (!string.IsNullOrEmpty(_report.logPath))
            {
                EditorGUILayout.SelectableLabel(_report.logPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var item in _report.items)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(L10n.Tr(item.status ?? "Keep") + " — " + (item.kind ?? ""), EditorUtility.FormatBytes(item.bytes));
                EditorGUILayout.SelectableLabel(item.path, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.LabelField(L10n.Tr(item.reason ?? ""), EditorStyles.wordWrappedLabel);
                if (!string.IsNullOrEmpty(item.reference))
                {
                    EditorGUILayout.LabelField(item.reference, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        void Execute(bool delete)
        {
            try
            {
                _message = null;
                _report = delete ? FluxParticleGenerationCleanup.CollectReviewed(_report) : FluxParticleGenerationCleanup.Analyze(root);
            }
            catch (Exception error)
            {
                _message = error.Message;
            }
        }
    }
}
