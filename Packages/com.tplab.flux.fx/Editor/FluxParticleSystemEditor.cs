using TpLab.Flux.FX.Editor.Preview;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor
{
    [CustomEditor(typeof(FluxParticleSystem))]
    public class FluxParticleSystemEditor : UnityEditor.Editor
    {
        const float ConeGizmoLength = 2.0f;
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

        SerializedObject _authoringObject;

        SerializedProperty _simulationSpace;
        SerializedProperty _playback;
        SerializedProperty _emission;
        SerializedProperty _lifetime;
        SerializedProperty _shape;
        SerializedProperty _initialVelocity;
        SerializedProperty _velocityOverLifetime;
        SerializedProperty _gravity;
        SerializedProperty _force;
        SerializedProperty _drag;
        SerializedProperty _noise;
        SerializedProperty _vortex;
        SerializedProperty _limitVelocity;
        SerializedProperty _render;

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
            _gravity = _authoringObject.FindProperty("gravity");
            _force = _authoringObject.FindProperty("force");
            _drag = _authoringObject.FindProperty("drag");
            _noise = _authoringObject.FindProperty("noise");
            _vortex = _authoringObject.FindProperty("vortex");
            _limitVelocity = _authoringObject.FindProperty("limitVelocity");
            _render = _authoringObject.FindProperty("render");

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
                EditorGUILayout.HelpBox("FluxParticleAuthoring is required on the same GameObject.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            _authoringObject.Update();

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

        void DrawPreview()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal();

                if (_preview != null && _preview.IsPlaying)
                {
                    if (GUILayout.Button("Stop"))
                    {
                        _preview.Stop();
                    }

                    if (GUILayout.Button("Restart"))
                    {
                        _authoringObject.ApplyModifiedProperties();
                        serializedObject.ApplyModifiedProperties();
                        _preview.Restart();
                    }
                }
                else
                {
                    if (GUILayout.Button("Preview"))
                    {
                        _authoringObject.ApplyModifiedProperties();
                        serializedObject.ApplyModifiedProperties();
                        _preview?.Play();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawMain()
        {
            DrawSectionHeader("MAIN");

            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("particleCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnAwake"), new GUIContent("Play On Awake"));
            EditorGUILayout.PropertyField(_playback.FindPropertyRelative("startDelay"), new GUIContent("Start Delay"));
            EditorGUILayout.PropertyField(_playback.FindPropertyRelative("duration"), new GUIContent("Duration"));
            EditorGUILayout.PropertyField(_playback.FindPropertyRelative("loop"), new GUIContent("Loop"));
            DrawLifetime();
            EditorGUILayout.PropertyField(_simulationSpace, new GUIContent("Simulation Space"));
        }

        void DrawEmission()
        {
            _emissionExpanded = DrawSectionHeader("EMISSION", _emissionExpanded, EmissionExpandedKey);
            if (!_emissionExpanded) return;

            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(_emission.FindPropertyRelative("rate"));
            EditorGUILayout.PropertyField(_emission.FindPropertyRelative("bursts"), true);
        }

        void DrawLifetime()
        {
            var mode = _lifetime.FindPropertyRelative("mode");

            EditorGUILayout.PropertyField(mode, new GUIContent("Start Lifetime Mode"));

            if ((FluxParticleStartLifetimeMode)mode.enumValueIndex == FluxParticleStartLifetimeMode.Constant)
            {
                EditorGUILayout.PropertyField(
                    _lifetime.FindPropertyRelative("lifetime"),
                    new GUIContent("Start Lifetime"));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _lifetime.FindPropertyRelative("min"),
                    new GUIContent("Start Lifetime Min"));
                EditorGUILayout.PropertyField(
                    _lifetime.FindPropertyRelative("max"),
                    new GUIContent("Start Lifetime Max"));
            }
        }

        void DrawShape()
        {
            _shapeExpanded = DrawSectionHeader("SHAPE", _shapeExpanded, ShapeExpandedKey);
            if (!_shapeExpanded) return;

            EditorGUILayout.Space(2);

            var type = _shape.FindPropertyRelative("type");
            var shapeType = (FluxParticleShapeType)type.enumValueIndex;

            EditorGUILayout.PropertyField(type);

            if (shapeType == FluxParticleShapeType.Sphere ||
                shapeType == FluxParticleShapeType.Hemisphere ||
                shapeType == FluxParticleShapeType.Circle)
            {
                EditorGUILayout.PropertyField(_shape.FindPropertyRelative("radius"));
            }
            else if (shapeType == FluxParticleShapeType.Box)
            {
                EditorGUILayout.PropertyField(_shape.FindPropertyRelative("size"));
            }
            else if (shapeType == FluxParticleShapeType.Cone)
            {
                EditorGUILayout.PropertyField(_shape.FindPropertyRelative("radius"));
                EditorGUILayout.PropertyField(_shape.FindPropertyRelative("angle"));
            }
        }

        void DrawVelocity()
        {
            _velocityExpanded = DrawSectionHeader("VELOCITY", _velocityExpanded, VelocityExpandedKey);
            if (!_velocityExpanded) return;

            EditorGUILayout.Space(2);

            DrawInitialSpeed();

            EditorGUILayout.Space();

            _velocityOverLifetimeExpanded = DrawModule(
                "Velocity over Lifetime",
                _velocityOverLifetime,
                _velocityOverLifetimeExpanded,
                VelocityOverLifetimeExpandedKey);

            _gravityExpanded = DrawModule(
                "Gravity",
                _gravity,
                _gravityExpanded,
                GravityExpandedKey);

            _forceExpanded = DrawModule(
                "Force",
                _force,
                _forceExpanded,
                ForceExpandedKey);

            _dragExpanded = DrawModule(
                "Drag",
                _drag,
                _dragExpanded,
                DragExpandedKey);

            _noiseExpanded = DrawModule(
                "Noise",
                _noise,
                _noiseExpanded,
                NoiseExpandedKey);

            _vortexExpanded = DrawModule(
                "Vortex",
                _vortex,
                _vortexExpanded,
                VortexExpandedKey);

            _limitVelocityExpanded = DrawModule(
                "Limit Velocity",
                _limitVelocity,
                _limitVelocityExpanded,
                LimitVelocityExpandedKey);
        }

        void DrawInitialSpeed()
        {
            var mode = _initialVelocity.FindPropertyRelative("mode");

            EditorGUILayout.PropertyField(mode, new GUIContent("Initial Speed Mode"));

            if ((FluxParticleInitialSpeedMode)mode.enumValueIndex == FluxParticleInitialSpeedMode.Constant)
            {
                EditorGUILayout.PropertyField(
                    _initialVelocity.FindPropertyRelative("speed"),
                    new GUIContent("Initial Speed"));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _initialVelocity.FindPropertyRelative("min"),
                    new GUIContent("Initial Speed Min"));
                EditorGUILayout.PropertyField(
                    _initialVelocity.FindPropertyRelative("max"),
                    new GUIContent("Initial Speed Max"));
            }
        }

        void DrawRenderer()
        {
            _renderExpanded = DrawSectionHeader("RENDERER", _renderExpanded, RendererExpandedKey);
            if (!_renderExpanded) return;

            EditorGUILayout.Space(2);

            var mode = _render.FindPropertyRelative("mode");
            var mesh = _render.FindPropertyRelative("mesh");

            EditorGUILayout.PropertyField(mode);

            if ((FluxParticleRenderMode)mode.enumValueIndex == FluxParticleRenderMode.Mesh)
            {
                EditorGUILayout.PropertyField(mesh);

                if (mesh.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Mesh is required for Mesh render mode.", MessageType.Error);
                }
            }

            EditorGUILayout.PropertyField(_render.FindPropertyRelative("material"));

            EditorGUILayout.Space();

            DrawStartColor();
            DrawStartSize();

            if ((FluxParticleRenderMode)mode.enumValueIndex == FluxParticleRenderMode.Mesh)
            {
                EditorGUILayout.PropertyField(_render.FindPropertyRelative("startRotation"));
            }

            EditorGUILayout.Space();

            DrawColorOverLifetime();
            DrawSizeOverLifetime();

            if ((FluxParticleRenderMode)mode.enumValueIndex == FluxParticleRenderMode.Billboard)
            {
                DrawRotationOverLifetime();
            }
        }

        void DrawStartSize()
        {
            var startSizeMode = _render.FindPropertyRelative("startSizeMode");

            EditorGUILayout.PropertyField(
                startSizeMode,
                new GUIContent("Start Size Mode"));

            if ((FluxParticleStartSizeMode)startSizeMode.enumValueIndex == FluxParticleStartSizeMode.Constant)
            {
                EditorGUILayout.PropertyField(_render.FindPropertyRelative("startSize"));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startSizeMin"),
                    new GUIContent("Start Size Min"));
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startSizeMax"),
                    new GUIContent("Start Size Max"));
            }
        }

        void DrawStartColor()
        {
            var startColorMode = _render.FindPropertyRelative("startColorMode");

            EditorGUILayout.PropertyField(
                startColorMode,
                new GUIContent("Start Color Mode"));

            if ((FluxParticleStartColorMode)startColorMode.enumValueIndex == FluxParticleStartColorMode.Constant)
            {
                EditorGUILayout.PropertyField(_render.FindPropertyRelative("startColor"));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startColorMin"),
                    new GUIContent("Start Color Min"));
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startColorMax"),
                    new GUIContent("Start Color Max"));
            }
        }

        void DrawColorOverLifetime()
        {
            var module = _render.FindPropertyRelative("colorOverLifetime");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _colorOverLifetimeExpanded = DrawModuleHeader(
                "Color over Lifetime",
                module,
                _colorOverLifetimeExpanded,
                ColorOverLifetimeExpandedKey);

            if (_colorOverLifetimeExpanded)
            {
                var enabled = module.FindPropertyRelative("enabled");

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUI.indentLevel++;

                    var mode = module.FindPropertyRelative("mode");
                    EditorGUILayout.PropertyField(mode);

                    if ((FluxParticleColorOverLifetimeMode)mode.enumValueIndex == FluxParticleColorOverLifetimeMode.Linear)
                    {
                        EditorGUILayout.PropertyField(module.FindPropertyRelative("endColor"));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(module.FindPropertyRelative("gradient"));
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }

        void DrawSizeOverLifetime()
        {
            var module = _render.FindPropertyRelative("sizeOverLifetime");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _sizeOverLifetimeExpanded = DrawModuleHeader(
                "Size over Lifetime",
                module,
                _sizeOverLifetimeExpanded,
                SizeOverLifetimeExpandedKey);

            if (_sizeOverLifetimeExpanded)
            {
                var enabled = module.FindPropertyRelative("enabled");

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUI.indentLevel++;

                    var mode = module.FindPropertyRelative("mode");
                    EditorGUILayout.PropertyField(mode);

                    if ((FluxParticleSizeOverLifetimeMode)mode.enumValueIndex == FluxParticleSizeOverLifetimeMode.Linear)
                    {
                        EditorGUILayout.PropertyField(module.FindPropertyRelative("endSize"));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(module.FindPropertyRelative("curve"));
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }

        void DrawRotationOverLifetime()
        {
            var module = _render.FindPropertyRelative("rotationOverLifetime");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _rotationOverLifetimeExpanded = DrawModuleHeader(
                "Rotation over Lifetime",
                module,
                _rotationOverLifetimeExpanded,
                RotationOverLifetimeExpandedKey);

            if (_rotationOverLifetimeExpanded)
            {
                var enabled = module.FindPropertyRelative("enabled");

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUI.indentLevel++;

                    var mode = module.FindPropertyRelative("mode");
                    EditorGUILayout.PropertyField(mode);

                    if ((FluxParticleRotationOverLifetimeMode)mode.enumValueIndex == FluxParticleRotationOverLifetimeMode.Linear)
                    {
                        EditorGUILayout.PropertyField(module.FindPropertyRelative("endRotation"));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(module.FindPropertyRelative("endRotation"));
                        EditorGUILayout.PropertyField(module.FindPropertyRelative("curve"));
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }

        void DrawSectionHeader(string label)
        {
            DrawSectionHeaderInternal(label, false, false);
        }

        bool DrawSectionHeader(string label, bool expanded, string sessionStateKey)
        {
            var nextExpanded = DrawSectionHeaderInternal(label, true, expanded);

            if (nextExpanded != expanded)
            {
                SessionState.SetBool(sessionStateKey, nextExpanded);
            }

            return nextExpanded;
        }

        bool DrawSectionHeaderInternal(string label, bool collapsible, bool expanded)
        {
            InitializeSectionHeaderStyles();

            EditorGUILayout.Space(SectionSpacing);

            var rect = GUILayoutUtility.GetRect(0, SectionHeaderHeight, GUILayout.ExpandWidth(true));
            rect.x = 0;
            rect.width = EditorGUIUtility.currentViewWidth;

            var backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.76f, 0.76f, 0.76f);

            var borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.10f, 0.10f, 0.10f)
                : new Color(0.58f, 0.58f, 0.58f);

            EditorGUI.DrawRect(rect, backgroundColor);

            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, rect.width, 1),
                borderColor);

            EditorGUI.DrawRect(
                new Rect(rect.x, rect.yMax - 1, rect.width, 1),
                borderColor);

            var chevronRect = new Rect(
                rect.x + 12,
                rect.y,
                16,
                rect.height);

            if (collapsible)
            {
                GUI.Label(
                    chevronRect,
                    expanded ? "▼" : "▶",
                    _sectionHeaderChevronStyle);

                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            }

            var labelRect = new Rect(
                rect.x + 34,
                rect.y,
                rect.width - 46,
                rect.height);

            GUI.Label(labelRect, label, _sectionHeaderLabelStyle);

            if (!collapsible) return expanded;

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                Event.current.Use();
                GUI.changed = true;
            }

            return expanded;
        }

        void InitializeSectionHeaderStyles()
        {
            if (_sectionHeaderLabelStyle != null) return;

            _sectionHeaderLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            _sectionHeaderLabelStyle.alignment = TextAnchor.MiddleLeft;
            _sectionHeaderLabelStyle.fontSize = 11;

            _sectionHeaderChevronStyle = new GUIStyle(EditorStyles.miniLabel);
            _sectionHeaderChevronStyle.alignment = TextAnchor.MiddleCenter;
        }

        bool DrawModule(string label, SerializedProperty module, bool expanded, string sessionStateKey)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            expanded = DrawModuleHeader(label, module, expanded, sessionStateKey);

            if (expanded)
            {
                var enabled = module.FindPropertyRelative("enabled");

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUI.indentLevel++;

                    var property = module.Copy();
                    var endProperty = property.GetEndProperty();

                    property.NextVisible(true);

                    while (property.NextVisible(false) && !SerializedProperty.EqualContents(property, endProperty))
                    {
                        if (property.name == "enabled")
                        {
                            continue;
                        }

                        EditorGUILayout.PropertyField(property, true);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();

            return expanded;
        }

        bool DrawModuleHeader(string label, SerializedProperty module, bool expanded, string sessionStateKey)
        {
            InitializeModuleHeaderStyles();

            var enabled = module.FindPropertyRelative("enabled");
            var headerRect = GUILayoutUtility.GetRect(0, ModuleHeaderHeight, GUILayout.ExpandWidth(true));

            var toggleRect = new Rect(
                headerRect.x + 2,
                headerRect.y + 2,
                18,
                headerRect.height - 4);

            var labelRect = new Rect(
                headerRect.x + 24,
                headerRect.y,
                headerRect.width - 26,
                headerRect.height);

            enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);

            if (labelRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
            {
                var hoverColor = EditorGUIUtility.isProSkin
                    ? new Color(1, 1, 1, 0.04f)
                    : new Color(0, 0, 0, 0.04f);

                EditorGUI.DrawRect(labelRect, hoverColor);
            }

            GUI.Label(labelRect, label, _moduleHeaderLabelStyle);
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                labelRect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                SessionState.SetBool(sessionStateKey, expanded);
                Event.current.Use();
                GUI.changed = true;
            }

            return expanded;
        }

        void InitializeModuleHeaderStyles()
        {
            if (_moduleHeaderLabelStyle != null) return;

            _moduleHeaderLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            _moduleHeaderLabelStyle.alignment = TextAnchor.MiddleLeft;
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
        }

        void DrawPointShapeGizmo()
        {
            var particleSystem = (FluxParticleSystem)target;
            var position = particleSystem.transform.position;
            var size = HandleUtility.GetHandleSize(position) * 0.08f;

            Handles.DotHandleCap(
                0,
                position,
                Quaternion.identity,
                size,
                EventType.Repaint);
        }

        void DrawSphereShapeGizmo()
        {
            var particleSystem = (FluxParticleSystem)target;
            var radius = _shape.FindPropertyRelative("radius");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius.floatValue);
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius.floatValue);

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, radius.floatValue);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawHemisphereShapeGizmo()
        {
            var particleSystem = (FluxParticleSystem)target;
            var radius = _shape.FindPropertyRelative("radius");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);
            Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right, 180, radius.floatValue);
            Handles.DrawWireArc(Vector3.zero, Vector3.right, Vector3.forward, -180, radius.floatValue);

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.ScaleSlider(
                radius.floatValue,
                Vector3.zero,
                Vector3.up,
                Quaternion.identity,
                radius.floatValue,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawCircleShapeGizmo()
        {
            var particleSystem = (FluxParticleSystem)target;
            var radius = _shape.FindPropertyRelative("radius");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.ScaleSlider(
                radius.floatValue,
                Vector3.zero,
                Vector3.up,
                Quaternion.Euler(90, 0, 0),
                radius.floatValue,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawBoxShapeGizmo()
        {
            var particleSystem = (FluxParticleSystem)target;
            var size = _shape.FindPropertyRelative("size");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            var currentSize = size.vector3Value;
            Handles.DrawWireCube(Vector3.zero, currentSize);

            EditorGUI.BeginChangeCheck();

            var halfSize = currentSize * 0.5f;

            var newHalfX = Handles.ScaleSlider(
                halfSize.x,
                Vector3.zero,
                Vector3.right,
                Quaternion.identity,
                halfSize.x,
                0);

            var newHalfY = Handles.ScaleSlider(
                halfSize.y,
                Vector3.zero,
                Vector3.up,
                Quaternion.identity,
                halfSize.y,
                0);

            var newHalfZ = Handles.ScaleSlider(
                halfSize.z,
                Vector3.zero,
                Vector3.forward,
                Quaternion.identity,
                halfSize.z,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                size.vector3Value = new Vector3(
                    Mathf.Max(0, newHalfX * 2.0f),
                    Mathf.Max(0, newHalfY * 2.0f),
                    Mathf.Max(0, newHalfZ * 2.0f));
                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }

        void DrawConeShapeGizmo()
        {
            var particleSystem = (FluxParticleSystem)target;
            var radius = _shape.FindPropertyRelative("radius");
            var angle = _shape.FindPropertyRelative("angle");
            var previousMatrix = Handles.matrix;

            Handles.matrix = particleSystem.transform.localToWorldMatrix;

            var angleRadians = angle.floatValue * Mathf.Deg2Rad;
            var directionRadius = Mathf.Sin(angleRadians) * ConeGizmoLength;
            var directionHeight = Mathf.Cos(angleRadians) * ConeGizmoLength;
            var endRadius = radius.floatValue + directionRadius;
            var directionCenter = Vector3.up * directionHeight;

            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius.floatValue);
            Handles.DrawWireDisc(directionCenter, Vector3.up, endRadius);

            Handles.DrawLine(
                new Vector3(radius.floatValue, 0, 0),
                new Vector3(endRadius, directionHeight, 0));
            Handles.DrawLine(
                new Vector3(-radius.floatValue, 0, 0),
                new Vector3(-endRadius, directionHeight, 0));
            Handles.DrawLine(
                new Vector3(0, 0, radius.floatValue),
                new Vector3(0, directionHeight, endRadius));
            Handles.DrawLine(
                new Vector3(0, 0, -radius.floatValue),
                new Vector3(0, directionHeight, -endRadius));

            EditorGUI.BeginChangeCheck();

            var newRadius = Handles.ScaleSlider(
                radius.floatValue,
                Vector3.zero,
                Vector3.right,
                Quaternion.identity,
                radius.floatValue,
                0);

            var angleBasePosition = new Vector3(radius.floatValue, 0, 0);
            var angleHandlePosition = new Vector3(endRadius, directionHeight, 0);
            var newAngleHandlePosition = Handles.Slider(
                angleHandlePosition,
                Vector3.right,
                HandleUtility.GetHandleSize(angleHandlePosition) * 0.08f,
                Handles.DotHandleCap,
                0);

            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0, newRadius);

                var handleDirection = newAngleHandlePosition - angleBasePosition;

                if (handleDirection.sqrMagnitude > 0)
                {
                    var newAngle = Vector3.Angle(Vector3.up, handleDirection);
                    angle.floatValue = Mathf.Clamp(newAngle, 0, 90);
                }

                _authoringObject.ApplyModifiedProperties();
            }

            Handles.matrix = previousMatrix;
        }
    }
}