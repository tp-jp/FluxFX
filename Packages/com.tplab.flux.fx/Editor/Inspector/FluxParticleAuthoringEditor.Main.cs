using TpLab.Flux.FX.Editor.Localization;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleAuthoringEditor
    {
        void DrawMain()
        {
            DrawSectionHeader(L10n.Tr("MAIN"));

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("particleCount"), new GUIContent(L10n.Tr("Particle Count")));

            DrawEnum(
                _simulationSpace,
                "Simulation Space",
                "Determines whether particle positions and movement are simulated in local or world space.");

            DrawEffectScale();
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnAwake"), new GUIContent(L10n.Tr("Play On Awake")));
            EditorGUILayout.PropertyField(_playback.FindPropertyRelative("startDelay"), new GUIContent(L10n.Tr("Start Delay")));

            EditorGUILayout.PropertyField(
                _playback.FindPropertyRelative("duration"),
                new GUIContent(
                    L10n.Tr("Duration"),
                    L10n.Tr("Sets how long particles are emitted during each cycle.")));

            EditorGUILayout.PropertyField(_playback.FindPropertyRelative("loop"), new GUIContent(L10n.Tr("Loop")));

            EditorGUILayout.Space(4);

            DrawLifetime();
            DrawStartSpeed();
            DrawStartColor();
            DrawStartSize();

            DrawStartRotation();
        }

        void DrawEffectScale()
        {
            var property = serializedObject.FindProperty("effectScale");
            var rect = EditorGUILayout.GetControlRect();
            var label = new GUIContent(L10n.Tr("Effect Scale"),
                L10n.Tr("Scales positions, particle sizes and effect-relative distances without changing Transform Scale or simulation time. Editable only outside Play Mode."));
            EditorGUI.BeginProperty(rect, label, property);
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.DelayedFloatField(rect, label, property.floatValue);
            if (EditorGUI.EndChangeCheck() && FluxParticleCoordinates.IsValidScale(value) &&
                (_simulationSpace.intValue != (int)FluxParticleSimulationSpace.World || value == 1))
            {
                property.floatValue = value;
            }
            EditorGUI.EndProperty();
            if (_simulationSpace.intValue == (int)FluxParticleSimulationSpace.World)
            {
                EditorGUILayout.HelpBox(L10n.Tr("World simulation currently requires Effect Scale 1."),
                    property.floatValue == 1 ? MessageType.Info : MessageType.Error);
            }
        }

        void DrawStartRotation()
        {
            var mode = _render.FindPropertyRelative("startRotationMode");
            DrawEnum(mode, "Start Rotation Mode");
            if ((FluxParticleStartRotationMode)mode.intValue == FluxParticleStartRotationMode.Constant)
            {
                DrawStartRotationValue("startRotation", "Start Rotation");
            }
            else
            {
                DrawStartRotationValue("startRotationMin", "Start Rotation Min");
                DrawStartRotationValue("startRotationMax", "Start Rotation Max");
            }
        }

        void DrawStartRotationValue(string propertyName, string label)
        {
            var property = _render.FindPropertyRelative(propertyName);
            var rotation = property.vector3Value;
            var renderMode = (FluxParticleRenderMode)_render.FindPropertyRelative("mode").intValue;
            if (renderMode == FluxParticleRenderMode.Mesh || rotation.x != 0 || rotation.y != 0)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(L10n.Tr(label)));
            }
            else
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("z"), new GUIContent(L10n.Tr(label)));
            }
        }
        void DrawLifetime()
        {
            var mode = _lifetime.FindPropertyRelative("mode");

            DrawEnum(mode, "Start Lifetime Mode");

            if ((FluxParticleStartLifetimeMode)mode.enumValueIndex == FluxParticleStartLifetimeMode.Constant)
            {
                EditorGUILayout.PropertyField(
                    _lifetime.FindPropertyRelative("lifetime"),
                    new GUIContent(L10n.Tr("Start Lifetime")));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _lifetime.FindPropertyRelative("min"),
                    new GUIContent(L10n.Tr("Start Lifetime Min")));

                EditorGUILayout.PropertyField(
                    _lifetime.FindPropertyRelative("max"),
                    new GUIContent(L10n.Tr("Start Lifetime Max")));
            }
        }

        void DrawStartSpeed()
        {
            var mode = _initialVelocity.FindPropertyRelative("mode");

            DrawEnum(mode, "Start Speed Mode");

            if ((FluxParticleStartSpeedMode)mode.enumValueIndex == FluxParticleStartSpeedMode.Constant)
            {
                DrawStartSpeedProperty("speed", "Start Speed");
            }
            else
            {
                DrawStartSpeedProperty("min", "Start Speed Min");
                DrawStartSpeedProperty("max", "Start Speed Max");
            }
        }

        void DrawStartSpeedProperty(string name, string label)
        {
            var property = _initialVelocity.FindPropertyRelative(name);
            if (TryDrawLiveProperty(property, labelOverride: label)) return;
            EditorGUILayout.PropertyField(property, new GUIContent(L10n.Tr(label)));
        }
    }
}
