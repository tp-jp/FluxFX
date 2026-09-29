using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor
{
    [CustomEditor(typeof(FluxParticleSystem))]
    public class FluxParticleSystemEditor : UnityEditor.Editor
    {
        bool _velocityExpanded = true;

        SerializedObject _authoringObject;

        SerializedProperty _gravity;
        SerializedProperty _drag;
        SerializedProperty _noise;
        SerializedProperty _vortex;

        void OnEnable()
        {
            var particleSystem = (FluxParticleSystem)target;
            var authoring = particleSystem.GetComponent<FluxParticleAuthoring>();

            if (authoring == null) return;

            _authoringObject = new SerializedObject(authoring);

            _gravity = _authoringObject.FindProperty("gravity");
            _drag = _authoringObject.FindProperty("drag");
            _noise = _authoringObject.FindProperty("noise");
            _vortex = _authoringObject.FindProperty("vortex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawRuntimeSettings();

            if (_authoringObject == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "FluxParticleAuthoring is required on the same GameObject.",
                    MessageType.Error);

                serializedObject.ApplyModifiedProperties();
                return;
            }

            _authoringObject.Update();

            EditorGUILayout.Space();

            DrawVelocity();

            _authoringObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawRuntimeSettings()
        {
            EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("particleCount"));
        }

        void DrawVelocity()
        {
            _velocityExpanded = EditorGUILayout.Foldout(
                _velocityExpanded,
                "Velocity",
                true);

            if (!_velocityExpanded) return;

            EditorGUI.indentLevel++;

            DrawModule("Gravity", _gravity);
            DrawModule("Drag", _drag);
            DrawModule("Noise", _noise);
            DrawModule("Vortex", _vortex);

            EditorGUI.indentLevel--;
        }

        void DrawModule(string label, SerializedProperty module)
        {
            var enabled = module.FindPropertyRelative("enabled");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            enabled.boolValue = EditorGUILayout.ToggleLeft(
                label,
                enabled.boolValue,
                EditorStyles.boldLabel);

            if (enabled.boolValue)
            {
                EditorGUI.indentLevel++;

                var property = module.Copy();
                var endProperty = property.GetEndProperty();

                property.NextVisible(true);

                while (property.NextVisible(false) &&
                       !SerializedProperty.EqualContents(property, endProperty))
                {
                    if (property.name == "enabled") continue;

                    EditorGUILayout.PropertyField(property, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }
}