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

        bool _shapeExpanded = true;
        bool _velocityExpanded = true;
        bool _renderExpanded = true;

        SerializedObject _authoringObject;

        SerializedProperty _simulationSpace;
        SerializedProperty _emission;
        SerializedProperty _lifetime;
        SerializedProperty _shape;
        SerializedProperty _initialVelocity;
        SerializedProperty _gravity;
        SerializedProperty _force;
        SerializedProperty _drag;
        SerializedProperty _noise;
        SerializedProperty _vortex;
        SerializedProperty _limitVelocity;
        SerializedProperty _render;

        FluxParticlePreview _preview;

        void OnEnable()
        {
            var particleSystem = (FluxParticleSystem)target;
            var authoring = particleSystem.GetComponent<FluxParticleAuthoring>();
            if (authoring == null) return;

            _authoringObject = new SerializedObject(authoring);

            _simulationSpace = _authoringObject.FindProperty("simulationSpace");
            _emission = _authoringObject.FindProperty("emission");
            _lifetime = _authoringObject.FindProperty("lifetime");
            _shape = _authoringObject.FindProperty("shape");
            _initialVelocity = _authoringObject.FindProperty("initialVelocity");
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
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

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

            EditorGUILayout.HelpBox(
                "Preview v0: Point / Rate / Lifetime / Initial Speed / Start Color / Start Size / Billboard",
                MessageType.Info);

            EditorGUILayout.Space();
        }

        void DrawMain()
        {
            EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("particleCount"));
            DrawLifetime();
            EditorGUILayout.PropertyField(_simulationSpace, new GUIContent("Simulation Space"));
        }

        void DrawEmission()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Emission", EditorStyles.boldLabel);
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
            EditorGUILayout.Space();

            _shapeExpanded = EditorGUILayout.Foldout(_shapeExpanded, "Shape", true);
            if (!_shapeExpanded) return;

            EditorGUI.indentLevel++;

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

            EditorGUI.indentLevel--;
        }

        void DrawVelocity()
        {
            EditorGUILayout.Space();

            _velocityExpanded = EditorGUILayout.Foldout(_velocityExpanded, "Velocity", true);
            if (!_velocityExpanded) return;

            EditorGUI.indentLevel++;

            DrawInitialSpeed();

            EditorGUILayout.Space();

            DrawModule("Gravity", _gravity);
            DrawModule("Force", _force);
            DrawModule("Drag", _drag);
            DrawModule("Noise", _noise);
            DrawModule("Vortex", _vortex);
            DrawModule("Limit Velocity", _limitVelocity);

            EditorGUI.indentLevel--;
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
            EditorGUILayout.Space();

            _renderExpanded = EditorGUILayout.Foldout(_renderExpanded, "Renderer", true);
            if (!_renderExpanded) return;

            EditorGUI.indentLevel++;

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

            EditorGUI.indentLevel--;
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
            var enabled = module.FindPropertyRelative("enabled");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            enabled.boolValue = EditorGUILayout.ToggleLeft(
                "Color over Lifetime",
                enabled.boolValue,
                EditorStyles.boldLabel);

            if (enabled.boolValue)
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

            EditorGUILayout.EndVertical();
        }

        void DrawSizeOverLifetime()
        {
            var module = _render.FindPropertyRelative("sizeOverLifetime");
            var enabled = module.FindPropertyRelative("enabled");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            enabled.boolValue = EditorGUILayout.ToggleLeft(
                "Size over Lifetime",
                enabled.boolValue,
                EditorStyles.boldLabel);

            if (enabled.boolValue)
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

            EditorGUILayout.EndVertical();
        }

        void DrawRotationOverLifetime()
        {
            var module = _render.FindPropertyRelative("rotationOverLifetime");
            var enabled = module.FindPropertyRelative("enabled");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            enabled.boolValue = EditorGUILayout.ToggleLeft(
                "Rotation over Lifetime",
                enabled.boolValue,
                EditorStyles.boldLabel);

            if (enabled.boolValue)
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

            EditorGUILayout.EndVertical();
        }

        void DrawModule(string label, SerializedProperty module)
        {
            var enabled = module.FindPropertyRelative("enabled");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            enabled.boolValue = EditorGUILayout.ToggleLeft(label, enabled.boolValue, EditorStyles.boldLabel);

            if (enabled.boolValue)
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

            EditorGUILayout.EndVertical();
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