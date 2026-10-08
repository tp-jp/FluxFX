using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        void DrawVelocity()
        {
            _velocityExpanded = DrawSectionHeader(L10n.Tr("VELOCITY"), _velocityExpanded, VelocityExpandedKey);
            if (!_velocityExpanded) return;

            EditorGUILayout.Space(2);

            _velocityOverLifetimeExpanded = DrawModule(
                L10n.Tr("Velocity over Lifetime"),
                _velocityOverLifetime,
                _velocityOverLifetimeExpanded,
                VelocityOverLifetimeExpandedKey);

            DrawGravityModule();

            _forceExpanded = DrawModule(
                L10n.Tr("Force"),
                _force,
                _forceExpanded,
                ForceExpandedKey);

            _dragExpanded = DrawModule(
                L10n.Tr("Drag"),
                _drag,
                _dragExpanded,
                DragExpandedKey);

            _noiseExpanded = DrawModule(
                L10n.Tr("Noise"),
                _noise,
                _noiseExpanded,
                NoiseExpandedKey);

            _vortexExpanded = DrawModule(
                L10n.Tr("Vortex"),
                _vortex,
                _vortexExpanded,
                VortexExpandedKey);

            _limitVelocityExpanded = DrawModule(
                L10n.Tr("Limit Velocity"),
                _limitVelocity,
                _limitVelocityExpanded,
                LimitVelocityExpandedKey);
        }

        void DrawGravityModule()
        {
            var moduleIndex = FindGravityModuleIndex();

            if (moduleIndex < 0)
            {
                _gravityExpanded = DrawModule(
                    L10n.Tr("Gravity"),
                    _gravity,
                    _gravityExpanded,
                    GravityExpandedKey);
            }
            else
            {
                var module = _modules.GetArrayElementAtIndex(moduleIndex);

                _gravityExpanded = DrawModule(
                    L10n.Tr("Gravity"),
                    module,
                    _gravityExpanded,
                    GravityExpandedKey);
            }

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(moduleIndex >= 0))
            {
                if (GUILayout.Button("Add Gravity Module", GUILayout.Width(160)))
                {
                    AddGravityModule();
                }
            }

            using (new EditorGUI.DisabledScope(moduleIndex < 0))
            {
                if (GUILayout.Button("Remove Module", GUILayout.Width(120)))
                {
                    RemoveGravityModule(moduleIndex);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        int FindGravityModuleIndex()
        {
            if (_modules == null) return -1;

            for (var i = 0; i < _modules.arraySize; i++)
            {
                var module = _modules.GetArrayElementAtIndex(i);

                if (module.managedReferenceValue is FluxParticleGravityModule)
                {
                    return i;
                }
            }

            return -1;
        }

        void AddGravityModule()
        {
            if (_modules == null || FindGravityModuleIndex() >= 0) return;

            var index = _modules.arraySize;
            _modules.arraySize++;

            var module = _modules.GetArrayElementAtIndex(index);
            module.managedReferenceValue = new FluxParticleGravityModule();

            // 従来設定を引き継ぎ、Module追加だけで挙動が変わらないようにする。
            module.FindPropertyRelative("enabled").boolValue =
                _gravity.FindPropertyRelative("enabled").boolValue;

            module.FindPropertyRelative("gravity").vector3Value =
                _gravity.FindPropertyRelative("gravity").vector3Value;

            _gravityExpanded = true;
            SessionState.SetBool(GravityExpandedKey, true);
        }

        void RemoveGravityModule(int index)
        {
            if (_modules == null || index < 0) return;

            _modules.DeleteArrayElementAtIndex(index);
        }
    }
}