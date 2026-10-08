using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    [CustomEditor(typeof(FluxParticleSystem))]
    public partial class FluxParticleSystemEditor : UnityEditor.Editor
    {
        const float SectionHeaderHeight = 30.0f;
        const float SectionSpacing = 6.0f;
        const float ModuleHeaderHeight = 22.0f;

        const string SessionStatePrefix = "TpLab.FluxFX.ParticleSystemInspector.";

        const string EmissionExpandedKey = SessionStatePrefix + "EmissionExpanded";
        const string ShapeExpandedKey = SessionStatePrefix + "ShapeExpanded";
        const string VelocityExpandedKey = SessionStatePrefix + "VelocityExpanded";
        const string RendererExpandedKey = SessionStatePrefix + "RendererExpanded";

        const string VelocityOverLifetimeExpandedKey = SessionStatePrefix + "VelocityOverLifetimeExpanded";
        const string GravityExpandedKey = SessionStatePrefix + "GravityExpanded";
        const string ForceExpandedKey = SessionStatePrefix + "ForceExpanded";
        const string DragExpandedKey = SessionStatePrefix + "DragExpanded";
        const string NoiseExpandedKey = SessionStatePrefix + "NoiseExpanded";
        const string VortexExpandedKey = SessionStatePrefix + "VortexExpanded";
        const string LimitVelocityExpandedKey = SessionStatePrefix + "LimitVelocityExpanded";

        const string ColorOverLifetimeExpandedKey = SessionStatePrefix + "ColorOverLifetimeExpanded";
        const string SizeOverLifetimeExpandedKey = SessionStatePrefix + "SizeOverLifetimeExpanded";
        const string RotationOverLifetimeExpandedKey = SessionStatePrefix + "RotationOverLifetimeExpanded";
        const string TextureSheetAnimationExpandedKey = SessionStatePrefix + "TextureSheetAnimationExpanded";

        static readonly string[] LanguageNames =
        {
            "English",
            "日本語"
        };

        bool _emissionExpanded;
        bool _shapeExpanded;
        bool _velocityExpanded;
        bool _renderExpanded;

        bool _velocityOverLifetimeExpanded;
        bool _gravityExpanded;
        bool _forceExpanded;
        bool _dragExpanded;
        bool _noiseExpanded;
        bool _vortexExpanded;
        bool _limitVelocityExpanded;

        bool _colorOverLifetimeExpanded;
        bool _sizeOverLifetimeExpanded;
        bool _rotationOverLifetimeExpanded;
        bool _textureSheetAnimationExpanded;

        SerializedObject _authoringObject;

        SerializedProperty _simulationSpace;
        SerializedProperty _playback;
        SerializedProperty _emission;
        SerializedProperty _lifetime;
        SerializedProperty _shape;
        SerializedProperty _initialVelocity;
        SerializedProperty _velocityOverLifetime;
        SerializedProperty _force;
        SerializedProperty _drag;
        SerializedProperty _noise;
        SerializedProperty _vortex;
        SerializedProperty _limitVelocity;
        SerializedProperty _render;
        SerializedProperty _modules;

        ReorderableList _burstList;

        FluxParticlePreview _preview;

        GUIStyle _sectionHeaderLabelStyle;
        GUIStyle _sectionHeaderChevronStyle;
        GUIStyle _moduleHeaderLabelStyle;

        void OnEnable()
        {
            LoadExpandedStates();

            var particleSystem = (FluxParticleSystem)target;
            var authoring = particleSystem.GetComponent<FluxParticleAuthoring>();
            if (authoring == null) return;

            _authoringObject = new SerializedObject(authoring);

            _simulationSpace = _authoringObject.FindProperty("simulationSpace");
            _playback = _authoringObject.FindProperty("playback");
            _emission = _authoringObject.FindProperty("emission");
            _lifetime = _authoringObject.FindProperty("lifetime");
            _shape = _authoringObject.FindProperty("shape");
            _initialVelocity = _authoringObject.FindProperty("initialVelocity");
            _velocityOverLifetime = _authoringObject.FindProperty("velocityOverLifetime");
            _force = _authoringObject.FindProperty("force");
            _drag = _authoringObject.FindProperty("drag");
            _noise = _authoringObject.FindProperty("noise");
            _vortex = _authoringObject.FindProperty("vortex");
            _limitVelocity = _authoringObject.FindProperty("limitVelocity");
            _render = _authoringObject.FindProperty("render");
            _modules = _authoringObject.FindProperty("modules");

            InitializeBurstList();

            _preview = new FluxParticlePreview(particleSystem, authoring);
        }

        void OnDisable()
        {
            _preview?.Dispose();
            _preview = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_authoringObject == null)
            {
                EditorGUILayout.HelpBox(L10n.Tr("FluxParticleAuthoring is required on the same GameObject."), MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            _authoringObject.Update();

            DrawLanguage();
            DrawPreview();
            DrawMain();
            DrawEmission();
            DrawShape();
            DrawVelocity();
            DrawRenderer();

            _authoringObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            if (_authoringObject == null) return;

            _authoringObject.Update();

            var type = (FluxParticleShapeType)_shape.FindPropertyRelative("type").enumValueIndex;

            if (type == FluxParticleShapeType.Point)
            {
                DrawPointShapeGizmo();
            }
            else if (type == FluxParticleShapeType.Sphere)
            {
                DrawSphereShapeGizmo();
            }
            else if (type == FluxParticleShapeType.Hemisphere)
            {
                DrawHemisphereShapeGizmo();
            }
            else if (type == FluxParticleShapeType.Circle)
            {
                DrawCircleShapeGizmo();
            }
            else if (type == FluxParticleShapeType.Box)
            {
                DrawBoxShapeGizmo();
            }
            else if (type == FluxParticleShapeType.Cone)
            {
                DrawConeShapeGizmo();
            }
        }

        void DrawLanguage()
        {
            var language = L10n.Language;

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(
                "Language / 言語",
                GUILayout.Width(100));

            var selectedIndex = EditorGUILayout.Popup(
                (int)language,
                LanguageNames,
                GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            if (selectedIndex == (int)language) return;

            L10n.Language = (Localization.Language)selectedIndex;
            Repaint();
        }

        void DrawPreview()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal();

                if (_preview != null && _preview.IsPlaying)
                {
                    if (GUILayout.Button(L10n.Tr("Stop")))
                    {
                        _preview.Stop();
                    }

                    if (GUILayout.Button(L10n.Tr("Restart")))
                    {
                        _authoringObject.ApplyModifiedProperties();
                        serializedObject.ApplyModifiedProperties();
                        _preview.Restart();
                    }
                }
                else
                {
                    if (GUILayout.Button(L10n.Tr("Preview")))
                    {
                        _authoringObject.ApplyModifiedProperties();
                        serializedObject.ApplyModifiedProperties();
                        _preview?.Play();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        void LoadExpandedStates()
        {
            _emissionExpanded = SessionState.GetBool(EmissionExpandedKey, false);
            _shapeExpanded = SessionState.GetBool(ShapeExpandedKey, false);
            _velocityExpanded = SessionState.GetBool(VelocityExpandedKey, false);
            _renderExpanded = SessionState.GetBool(RendererExpandedKey, false);

            _velocityOverLifetimeExpanded = SessionState.GetBool(VelocityOverLifetimeExpandedKey, false);
            _gravityExpanded = SessionState.GetBool(GravityExpandedKey, false);
            _forceExpanded = SessionState.GetBool(ForceExpandedKey, false);
            _dragExpanded = SessionState.GetBool(DragExpandedKey, false);
            _noiseExpanded = SessionState.GetBool(NoiseExpandedKey, false);
            _vortexExpanded = SessionState.GetBool(VortexExpandedKey, false);
            _limitVelocityExpanded = SessionState.GetBool(LimitVelocityExpandedKey, false);

            _colorOverLifetimeExpanded = SessionState.GetBool(ColorOverLifetimeExpandedKey, false);
            _sizeOverLifetimeExpanded = SessionState.GetBool(SizeOverLifetimeExpandedKey, false);
            _rotationOverLifetimeExpanded = SessionState.GetBool(RotationOverLifetimeExpandedKey, false);
            _textureSheetAnimationExpanded = SessionState.GetBool(TextureSheetAnimationExpandedKey, false);
        }
    }
}