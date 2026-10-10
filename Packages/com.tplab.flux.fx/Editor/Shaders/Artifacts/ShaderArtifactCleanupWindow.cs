using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Shaders.Artifacts
{
    public sealed class ShaderArtifactCleanupWindow : EditorWindow
    {
        readonly ShaderArtifactCleanup _cleanup = new ShaderArtifactCleanup();
        readonly HashSet<string> _selectedShaders = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _selectedIncludes = new HashSet<string>(StringComparer.Ordinal);

        ShaderArtifactCleanupReport _report;
        Vector2 _scrollPosition;
        string _error;

        [MenuItem("Tools/FluxFX/Generated Shader Cleanup")]
        static void Open()
        {
            var window = GetWindow<ShaderArtifactCleanupWindow>("Shader Cleanup");
            window.minSize = new Vector2(560, 360);
            window.Refresh();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Generated Shader Cleanup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Deletes only selected generated artifacts. Save modified scenes before scanning. " +
                "Dynamically referenced shaders cannot always be detected, so review the candidates carefully.",
                MessageType.Info);

            if (GUILayout.Button("Analyze", GUILayout.Height(28)))
            {
                Refresh();
            }

            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }

            if (_report == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Protected Shaders: {_report.ProtectedShaderCount}");
            EditorGUILayout.LabelField($"Protected Includes: {_report.ProtectedIncludeCount}");

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawCandidates("Shader Candidates", _report.ShaderCandidates, _selectedShaders);
            EditorGUILayout.Space();
            DrawCandidates("Include Candidates", _report.IncludeCandidates, _selectedIncludes);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            var selectedCount = _selectedShaders.Count + _selectedIncludes.Count;

            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"Delete Selected ({selectedCount})", GUILayout.Height(30)))
                {
                    DeleteSelected();
                }
            }
        }

        static void DrawCandidates(string title, IReadOnlyList<string> candidates, HashSet<string> selection)
        {
            EditorGUILayout.LabelField($"{title}: {candidates.Count}", EditorStyles.boldLabel);

            foreach (var path in candidates)
            {
                var selected = selection.Contains(path);
                var next = EditorGUILayout.ToggleLeft(path, selected);

                if (next)
                {
                    selection.Add(path);
                }
                else
                {
                    selection.Remove(path);
                }
            }
        }

        void Refresh()
        {
            _selectedShaders.Clear();
            _selectedIncludes.Clear();
            _report = null;
            _error = null;

            try
            {
                _report = _cleanup.Analyze();
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Logger.LogError($"Shader cleanup analysis failed: {exception}");
            }

            Repaint();
        }

        void DeleteSelected()
        {
            var count = _selectedShaders.Count + _selectedIncludes.Count;

            if (!EditorUtility.DisplayDialog(
                    "Delete Generated Shader Artifacts",
                    $"Delete {count} selected generated artifacts?\n\nThis operation cannot be undone.",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            try
            {
                var shaderCount = _cleanup.DeleteShaders(_selectedShaders.ToArray());

                // Shader削除後に依存関係を再解析してからIncludeを削除する。
                var includeCount = _cleanup.DeleteIncludes(_selectedIncludes.ToArray());

                Logger.Log($"Shader cleanup completed. Shaders: {shaderCount}, Includes: {includeCount}");
            }
            catch (Exception exception)
            {
                Logger.LogError($"Shader cleanup failed: {exception}");
                EditorUtility.DisplayDialog("Shader Cleanup Failed", exception.Message, "OK");
            }
            finally
            {
                Refresh();
            }
        }
    }
}