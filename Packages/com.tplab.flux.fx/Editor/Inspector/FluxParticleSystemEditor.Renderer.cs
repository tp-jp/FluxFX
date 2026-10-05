using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor
{
    public partial class FluxParticleSystemEditor
    {
        void DrawRenderer()
        {
            _renderExpanded = DrawSectionHeader(L10n.Tr("RENDERER"), _renderExpanded, RendererExpandedKey);
            if (!_renderExpanded) return;

            EditorGUILayout.Space(2);

            var mode = _render.FindPropertyRelative("mode");
            var mesh = _render.FindPropertyRelative("mesh");

            DrawEnum(mode, "Mode");

            if ((FluxParticleRenderMode)mode.enumValueIndex == FluxParticleRenderMode.Mesh)
            {
                EditorGUILayout.PropertyField(mesh, new GUIContent(L10n.Tr("Mesh")));

                if (mesh.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(L10n.Tr("Mesh is required for Mesh render mode."), MessageType.Error);
                }
            }

            EditorGUILayout.PropertyField(_render.FindPropertyRelative("material"), new GUIContent(L10n.Tr("Material")));

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

            DrawEnum(startSizeMode, "Start Size Mode");

            if ((FluxParticleStartSizeMode)startSizeMode.enumValueIndex == FluxParticleStartSizeMode.Constant)
            {
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startSize"),
                    new GUIContent(L10n.Tr("Start Size")));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startSizeMin"),
                    new GUIContent(L10n.Tr("Start Size Min")));

                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startSizeMax"),
                    new GUIContent(L10n.Tr("Start Size Max")));
            }
        }

        void DrawStartColor()
        {
            var startColorMode = _render.FindPropertyRelative("startColorMode");

            DrawEnum(startColorMode, "Start Color Mode");

            if ((FluxParticleStartColorMode)startColorMode.enumValueIndex == FluxParticleStartColorMode.Constant)
            {
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startColor"),
                    new GUIContent(L10n.Tr("Start Color")));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startColorMin"),
                    new GUIContent(L10n.Tr("Start Color Min")));

                EditorGUILayout.PropertyField(
                    _render.FindPropertyRelative("startColorMax"),
                    new GUIContent(L10n.Tr("Start Color Max")));
            }
        }

        void DrawColorOverLifetime()
        {
            var module = _render.FindPropertyRelative("colorOverLifetime");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _colorOverLifetimeExpanded = DrawModuleHeader(
                L10n.Tr("Color over Lifetime"),
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
                    DrawEnum(mode);

                    if ((FluxParticleColorOverLifetimeMode)mode.enumValueIndex == FluxParticleColorOverLifetimeMode.Linear)
                    {
                        DrawProperty(module.FindPropertyRelative("endColor"));
                    }
                    else
                    {
                        DrawProperty(module.FindPropertyRelative("gradient"));
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
                L10n.Tr("Size over Lifetime"),
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
                    DrawEnum(mode);

                    if ((FluxParticleSizeOverLifetimeMode)mode.enumValueIndex == FluxParticleSizeOverLifetimeMode.Linear)
                    {
                        DrawProperty(module.FindPropertyRelative("endSize"));
                    }
                    else
                    {
                        DrawProperty(module.FindPropertyRelative("curve"));
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
                L10n.Tr("Rotation over Lifetime"),
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
                    DrawEnum(mode);

                    if ((FluxParticleRotationOverLifetimeMode)mode.enumValueIndex == FluxParticleRotationOverLifetimeMode.Linear)
                    {
                        DrawProperty(module.FindPropertyRelative("endRotation"));
                    }
                    else
                    {
                        DrawProperty(module.FindPropertyRelative("endRotation"));
                        DrawProperty(module.FindPropertyRelative("curve"));
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}