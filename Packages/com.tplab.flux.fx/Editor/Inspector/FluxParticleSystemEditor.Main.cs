using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
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

            var renderMode = (FluxParticleRenderMode)_render.FindPropertyRelative("mode").enumValueIndex;

            if (renderMode == FluxParticleRenderMode.Mesh)
            {
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startRotation"),
                    new GUIContent(L10n.Tr("Start Rotation")));
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
                EditorGUILayout.PropertyField(
                    _initialVelocity.FindPropertyRelative("speed"),
                    new GUIContent(L10n.Tr("Start Speed")));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _initialVelocity.FindPropertyRelative("min"),
                    new GUIContent(L10n.Tr("Start Speed Min")));

                EditorGUILayout.PropertyField(
                    _initialVelocity.FindPropertyRelative("max"),
                    new GUIContent(L10n.Tr("Start Speed Max")));
            }
        }
    }
}