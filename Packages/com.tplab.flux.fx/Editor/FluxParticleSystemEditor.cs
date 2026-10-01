using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor
{
    [CustomEditor(typeof(FluxParticleSystem))]
    public class FluxParticleSystemEditor : UnityEditor.Editor
    {
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
        SerializedProperty _drag;
        SerializedProperty _noise;
        SerializedProperty _vortex;
        SerializedProperty _render;

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
            _drag = _authoringObject.FindProperty("drag");
            _noise = _authoringObject.FindProperty("noise");
            _vortex = _authoringObject.FindProperty("vortex");
            _render = _authoringObject.FindProperty("render");
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

            DrawMain();
            DrawEmission();
            DrawShape();
            DrawVelocity();
            DrawRenderer();

            _authoringObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
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

            EditorGUILayout.PropertyField(type);

            if ((FluxParticleShapeType)type.enumValueIndex == FluxParticleShapeType.Sphere)
            {
                EditorGUILayout.PropertyField(_shape.FindPropertyRelative("radius"));
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
            DrawModule("Drag", _drag);
            DrawModule("Noise", _noise);
            DrawModule("Vortex", _vortex);

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
    }
}