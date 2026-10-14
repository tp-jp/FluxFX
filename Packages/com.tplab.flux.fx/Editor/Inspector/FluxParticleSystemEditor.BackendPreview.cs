using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Compilation;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        const string BackendExpandedKey = SessionStatePrefix + "BackendExpanded";
        FluxParticleBuildResult _backendPreview;
        string _backendPreviewSignature;
        string _backendPreviewError;
        FluxParticleEditorPreview _particlePreview;
        bool _previewPlaying = true;
        double _lastPreviewTime;
        bool _unityFocused;
        bool _previewRefreshAfterSave;

        internal static FluxParticleSystemEditor PreviewEditor { get; private set; }
        internal bool HasScenePreview => target != null && _authoringObject != null && !EditorApplication.isPlayingOrWillChangePlaymode;

        void DrawBackendInfo()
        {
            var expanded = SessionState.GetBool(BackendExpandedKey, false);
            var showDetails = DrawSectionHeader(L10n.Tr("BACKEND"), expanded, BackendExpandedKey);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                DisposeParticlePreview();
                _backendPreviewSignature = null;
                if (showDetails)
                {
                    EditorGUILayout.HelpBox(L10n.Tr("SceneView preview is available in Edit Mode. Play Mode uses the runtime renderer."), MessageType.Info);
                }
                return;
            }
            EnsureParticlePreview();
            var count = ((FluxParticleSystem)target).ParticleCount;
            if (!showDetails) return;
            if (_backendPreviewError != null)
            {
                EditorGUILayout.HelpBox(_backendPreviewError, MessageType.Error);
                return;
            }

            var shaders = _backendPreview.Shaders;
            var compilation = shaders.Compilation;
            var pages = compilation.StateLayout.Pages;
            var lookups = compilation.Providers.Values.Where(provider => provider.Kind == FluxParticleProviderKind.LookupTable).ToArray();
            var width = Math.Ceiling(Math.Sqrt(Math.Max(0, count)));
            var stateBytes = width * width * pages.Count * 2 * 16;
            EditorGUILayout.LabelField(L10n.Tr("State Pages"), pages.Count.ToString());
            EditorGUILayout.LabelField(L10n.Tr("State RenderTextures"), (pages.Count * 2).ToString());
            EditorGUILayout.LabelField(L10n.Tr("Estimated State Memory"), (stateBytes / (1024 * 1024)).ToString("F2") + " MiB");
            EditorGUILayout.LabelField(L10n.Tr("Simulation Passes"), shaders.Passes.Count.ToString());
            EditorGUILayout.LabelField(L10n.Tr("Clear Passes"), shaders.ClearSources.Count.ToString());
            EditorGUILayout.LabelField(L10n.Tr("Lookup Tables"), lookups.Length.ToString());

            foreach (var page in pages)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Page " + page.Id + " / " + page.Format, EditorStyles.boldLabel);
                foreach (var binding in page.Bindings)
                {
                    EditorGUILayout.LabelField("RGBA".Substring(binding.ChannelOffset, binding.ComponentCount) + ": " + binding.ValueId,
                        EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.EndVertical();
            }
            foreach (var pass in shaders.Passes)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Pass " + pass.Plan.Index + " → Page " + pass.Plan.OutputPageId + " / " + pass.Plan.Condition,
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(L10n.Tr("Page Inputs"), string.Join(", ", pass.Reads.Select(read =>
                    "Page " + read.PageId + " (" + read.Version + ")")), EditorStyles.wordWrappedLabel);
                DrawBackendOperations(L10n.Tr("Spawn Operations"), pass.Plan.SpawnOperations);
                DrawBackendOperations(L10n.Tr("Update Operations"), pass.Plan.UpdateOperations);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawBackendOperations(L10n.Tr("Render Operations"), compilation.Operations.Where(operation => operation.Phase == FluxParticlePhase.Render));
            foreach (var provider in lookups)
            {
                EditorGUILayout.LabelField(provider.ValueId, provider.LookupSampleCount + " " + L10n.Tr("samples"));
            }
            EditorGUILayout.EndVertical();
        }

        void EnsureParticlePreview()
        {
            if (!HasScenePreview) return;
            var authoring = (FluxParticleAuthoring)_authoringObject.targetObject;
            var count = ((FluxParticleSystem)target).ParticleCount;
            var signature = EditorJsonUtility.ToJson(authoring) + ":" + count;
            if (authoring.Render.Material != null)
            {
                signature += EditorJsonUtility.ToJson(authoring.Render.Material);
            }
            if (signature != _backendPreviewSignature || (_particlePreview == null && _backendPreviewError == null))
            {
                _backendPreviewSignature = signature;
                DisposeParticlePreview();
                _backendPreview = null;
                _backendPreviewError = null;
                try
                {
                    _backendPreview = FluxParticleBuildPipeline.Build(authoring);
                    _particlePreview = new FluxParticleEditorPreview(_backendPreview, authoring, Mathf.Clamp(count, 1, 4096));
                    _lastPreviewTime = EditorApplication.timeSinceStartup;
                    SceneView.RepaintAll();
                }
                catch (Exception exception)
                {
                    _backendPreviewError = exception.Message;
                }
            }
        }

        void InitializeBackendPreview()
        {
            _backendPreviewSignature = null;
            _unityFocused = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
            _lastPreviewTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += UpdateParticlePreview;
            Camera.onPreCull += DrawParticlePreviewInSceneView;
            PreviewEditor = this;
            EditorApplication.focusChanged += OnPreviewFocusChanged;
            EditorSceneManager.sceneSaved += OnPreviewSceneSaved;
            EditorApplication.playModeStateChanged += OnPreviewPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeParticlePreview;
        }

        void OnDisable()
        {
            EditorApplication.update -= UpdateParticlePreview;
            Camera.onPreCull -= DrawParticlePreviewInSceneView;
            if (PreviewEditor == this) PreviewEditor = null;
            EditorApplication.focusChanged -= OnPreviewFocusChanged;
            EditorSceneManager.sceneSaved -= OnPreviewSceneSaved;
            EditorApplication.playModeStateChanged -= OnPreviewPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeParticlePreview;
            DisposeParticlePreview();
            _backendPreviewSignature = null;
        }

        void DisposeParticlePreview()
        {
            if (_particlePreview == null) return;
            _particlePreview.Dispose();
            _particlePreview = null;
            SceneView.RepaintAll();
        }

        void UpdateParticlePreview()
        {
            if (!HasScenePreview) return;
            RefreshPreviewAfterSave();
            if (_particlePreview == null && _backendPreviewError == null)
            {
                EnsureParticlePreview();
            }
            if (_particlePreview == null || !_previewPlaying) return;
            var focused = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
            if (focused != _unityFocused)
            {
                OnPreviewFocusChanged(focused);
            }
            var now = EditorApplication.timeSinceStartup;
            if (!_unityFocused)
            {
                _lastPreviewTime = now;
                return;
            }
            var delta = now - _lastPreviewTime;
            if (delta < 1.0 / 30) return;
            _lastPreviewTime = now;
            try
            {
                _particlePreview.Step((float)Math.Min(delta, 0.05));
                _particlePreview.RequestAliveCount();
            }
            catch (Exception exception)
            {
                _backendPreviewError = exception.Message;
                DisposeParticlePreview();
            }
            SceneView.RepaintAll();
            Repaint();
        }

        void OnPreviewSceneSaved(Scene scene)
        {
            if (target == null || ((FluxParticleSystem)target).gameObject.scene != scene) return;
            _previewRefreshAfterSave = true;
            Repaint();
        }

        void RefreshPreviewAfterSave()
        {
            if (!_previewRefreshAfterSave) return;
            _previewRefreshAfterSave = false;
            try
            {
                if (_particlePreview != null)
                {
                    var replacement = _particlePreview.Recreate(_backendPreview);
                    DisposeParticlePreview();
                    _particlePreview = replacement;
                }
                else
                {
                    _backendPreviewSignature = null;
                    EnsureParticlePreview();
                }
                _lastPreviewTime = EditorApplication.timeSinceStartup;
            }
            catch (Exception exception)
            {
                _backendPreviewError = exception.Message;
                DisposeParticlePreview();
            }
            SceneView.RepaintAll();
            Repaint();
        }
        void OnPreviewFocusChanged(bool focused)
        {
            _unityFocused = focused;
            _lastPreviewTime = EditorApplication.timeSinceStartup;
            SceneView.RepaintAll();
            Repaint();
        }

        internal void DrawScenePreviewControls()
        {
            if (_particlePreview == null)
            {
                EditorGUILayout.HelpBox(_backendPreviewError ?? L10n.Tr("Preparing preview"), MessageType.Info);
                if (GUILayout.Button(L10n.Tr("Restart"), GUILayout.Width(288)))
                {
                    _backendPreviewSignature = null;
                    EnsureParticlePreview();
                    _previewPlaying = true;
                }
                return;
            }
            var action = -1;
            var buttonOptions = new[] { GUILayout.Width(96), GUILayout.Height(22) };
            EditorGUILayout.BeginHorizontal(GUILayout.Width(288));
            if (GUILayout.Button(L10n.Tr(_previewPlaying ? "Pause" : "Play"), EditorStyles.miniButtonLeft, buttonOptions))
            {
                action = 0;
            }
            if (GUILayout.Button(L10n.Tr("Restart"), EditorStyles.miniButtonMid, buttonOptions))
            {
                action = 1;
            }
            if (GUILayout.Button(L10n.Tr("Stop"), EditorStyles.miniButtonRight, buttonOptions))
            {
                action = 2;
            }
            EditorGUILayout.EndHorizontal();
            if (action >= 0)
            {
                if (action == 0)
                {
                    _previewPlaying = !_previewPlaying;
                }
                else
                {
                    _particlePreview.Restart();
                    _previewPlaying = action == 1;
                }
                _lastPreviewTime = EditorApplication.timeSinceStartup;
                SceneView.RepaintAll();
                Repaint();
            }
            GUILayout.Label(L10n.Tr("Playback Time") + ": " + _particlePreview.PlaybackTime.ToString("F2") + " s");
            var count = _particlePreview.AliveCount;
            GUILayout.Label(L10n.Tr("Alive particles") + ": " + (count < 0 ? "—" : count.ToString()));
            if (!_unityFocused) GUILayout.Label(L10n.Tr("Paused while Unity is unfocused"));
        }
        void OnPreviewPlayModeStateChanged(PlayModeStateChange state)
        {
            DisposeParticlePreview();
            _backendPreviewSignature = null;
            Repaint();
        }

        void DrawParticlePreviewInSceneView(Camera camera)
        {
            if (_particlePreview == null || camera.cameraType != CameraType.SceneView ||
                EditorApplication.isPlayingOrWillChangePlaymode || target == null) return;
            var particleSystem = (FluxParticleSystem)target;
            if (!particleSystem.gameObject.activeInHierarchy ||
                SceneVisibilityManager.instance.IsHidden(particleSystem.gameObject)) return;
            try
            {
                _particlePreview.Draw(camera, particleSystem.transform.localToWorldMatrix, particleSystem.gameObject.layer);
            }
            catch (Exception exception)
            {
                _backendPreviewError = exception.Message;
                DisposeParticlePreview();
                Repaint();
            }
        }

        static void DrawBackendOperations(string label, IEnumerable<FluxParticleOperationDefinition> operations)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            foreach (var operation in operations)
            {
                var dependency = operation.Dependencies.Count == 0 ? "" : " ← " + string.Join(", ", operation.Dependencies);
                EditorGUILayout.LabelField(operation.Id + dependency, EditorStyles.wordWrappedLabel);
            }
        }
    }
}
