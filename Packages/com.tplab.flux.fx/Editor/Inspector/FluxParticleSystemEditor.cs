using TpLab.Flux.FX.Editor.Localization;
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
        const string ModulesExpandedKey = SessionStatePrefix + "ModulesExpanded";
        const string VelocityUpdateExpandedKey = SessionStatePrefix + "VelocityUpdateExpanded";
        const string PositionUpdateExpandedKey = SessionStatePrefix + "PositionUpdateExpanded";
        const string RenderingModulesExpandedKey = SessionStatePrefix + "RenderingModulesExpanded";
        const string RendererExpandedKey = SessionStatePrefix + "RendererExpanded";

        const string ColorOverLifetimeExpandedKey = SessionStatePrefix + "ColorOverLifetimeExpanded";
        const string SizeOverLifetimeExpandedKey = SessionStatePrefix + "SizeOverLifetimeExpanded";
        const string RotationOverLifetimeExpandedKey = SessionStatePrefix + "RotationOverLifetimeExpanded";

        static readonly string[] LanguageNames =
        {
            "English",
            "日本語"
        };

        bool _emissionExpanded;
        bool _shapeExpanded;
        bool _modulesExpanded;
        bool _renderExpanded;

        bool _colorOverLifetimeExpanded;
        bool _sizeOverLifetimeExpanded;
        bool _rotationOverLifetimeExpanded;

        FluxParticleInspectorModuleRegistry _moduleRegistry;

        SerializedObject _authoringObject;

        SerializedProperty _simulationSpace;
        SerializedProperty _playback;
        SerializedProperty _emission;
        SerializedProperty _lifetime;
        SerializedProperty _shape;
        SerializedProperty _initialVelocity;
        SerializedProperty _render;
        SerializedProperty _modules;

        ReorderableList _burstList;

        GUIStyle _sectionHeaderLabelStyle;
        GUIStyle _sectionHeaderChevronStyle;
        GUIStyle _moduleHeaderLabelStyle;

        void OnEnable()
        {
            LoadExpandedStates();

            _moduleRegistry = new FluxParticleInspectorModuleRegistry();

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
            _render = _authoringObject.FindProperty("render");
            _modules = _authoringObject.FindProperty("modules");

            InitializeBurstList();
            InitializeBackendPreview();

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
            DrawMain();
            DrawEmission();
            DrawShape();
            DrawModules();
            DrawRenderer();

            _authoringObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
            DrawBackendInfo();
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

        void LoadExpandedStates()
        {
            _emissionExpanded = SessionState.GetBool(EmissionExpandedKey, false);
            _shapeExpanded = SessionState.GetBool(ShapeExpandedKey, false);
            _modulesExpanded = SessionState.GetBool(ModulesExpandedKey, true);
            _renderExpanded = SessionState.GetBool(RendererExpandedKey, false);

            _colorOverLifetimeExpanded = SessionState.GetBool(ColorOverLifetimeExpandedKey, false);
            _sizeOverLifetimeExpanded = SessionState.GetBool(SizeOverLifetimeExpandedKey, false);
            _rotationOverLifetimeExpanded = SessionState.GetBool(RotationOverLifetimeExpandedKey, false);
        }
    }
}