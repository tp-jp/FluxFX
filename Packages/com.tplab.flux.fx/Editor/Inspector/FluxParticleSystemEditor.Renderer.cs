
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
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

            EditorGUILayout.PropertyField(
                _render.FindPropertyRelative("material"),
                new GUIContent(L10n.Tr("Material")));

            DrawEnum(
                _render.FindPropertyRelative("blendMode"),
                "Blend Mode");

            EditorGUILayout.Space();

            DrawColorOverLifetime();
            DrawSizeOverLifetime();
            DrawRotationOverLifetime();

            _textureSheetAnimationExpanded = DrawOptionalModule<FluxParticleTextureSheetAnimationModule>(
                L10n.Tr("Texture Sheet Animation"),
                _textureSheetAnimationExpanded,
                TextureSheetAnimationExpandedKey);

            DrawAddModuleButton(RendererModuleDefinitions);
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
            var moduleIndex = FindModuleIndex(typeof(FluxParticleColorOverLifetimeModule));
            if (moduleIndex < 0) return;

            var module = _modules.GetArrayElementAtIndex(moduleIndex);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _colorOverLifetimeExpanded = DrawModuleHeader(
                L10n.Tr("Color over Lifetime"),
                module,
                _colorOverLifetimeExpanded,
                ColorOverLifetimeExpandedKey,
                () => ShowRemoveModuleMenu(typeof(FluxParticleColorOverLifetimeModule)));

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
            var moduleIndex = FindModuleIndex(typeof(FluxParticleSizeOverLifetimeModule));
            if (moduleIndex < 0) return;

            var module = _modules.GetArrayElementAtIndex(moduleIndex);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _sizeOverLifetimeExpanded = DrawModuleHeader(
                L10n.Tr("Size over Lifetime"),
                module,
                _sizeOverLifetimeExpanded,
                SizeOverLifetimeExpandedKey,
                () => ShowRemoveModuleMenu(typeof(FluxParticleSizeOverLifetimeModule)));

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
            var moduleIndex = FindModuleIndex(typeof(FluxParticleRotationOverLifetimeModule));
            if (moduleIndex < 0) return;

            var module = _modules.GetArrayElementAtIndex(moduleIndex);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _rotationOverLifetimeExpanded = DrawModuleHeader(
                L10n.Tr("Rotation over Lifetime"),
                module,
                _rotationOverLifetimeExpanded,
                RotationOverLifetimeExpandedKey,
                () => ShowRemoveModuleMenu(typeof(FluxParticleRotationOverLifetimeModule)));

            if (_rotationOverLifetimeExpanded)
            {
                var enabled = module.FindPropertyRelative("enabled");

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUI.indentLevel++;

                    var mode = module.FindPropertyRelative("mode");
                    DrawEnum(mode);

                    DrawProperty(module.FindPropertyRelative("endRotation"));

                    if ((FluxParticleRotationOverLifetimeMode)mode.enumValueIndex == FluxParticleRotationOverLifetimeMode.Curve)
                    {
                        DrawProperty(module.FindPropertyRelative("curve"));
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
