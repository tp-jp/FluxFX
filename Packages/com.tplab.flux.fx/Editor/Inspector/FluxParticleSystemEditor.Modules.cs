using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        void DrawModules()
        {
            _modulesExpanded = DrawSectionHeader(L10n.Tr("MODULES"), _modulesExpanded, ModulesExpandedKey);
            if (!_modulesExpanded) return;

            EditorGUILayout.Space(2);

            DrawModuleStage(FluxParticleExecutionStage.VelocityUpdate, "Velocity Update", VelocityUpdateExpandedKey);
            DrawModuleStage(FluxParticleExecutionStage.PositionUpdate, "Position Update", PositionUpdateExpandedKey);
            DrawModuleStage(FluxParticleExecutionStage.Rendering, "Rendering", RenderingModulesExpandedKey);
        }

        void DrawModuleStage(FluxParticleExecutionStage stage, string label, string expandedKey)
        {
            var definitions = _moduleRegistry.GetModules(stage);
            if (definitions.Count == 0) return;

            var expanded = SessionState.GetBool(expandedKey, true);
            var nextExpanded = EditorGUILayout.Foldout(expanded, L10n.Tr(label), true, EditorStyles.foldoutHeader);

            if (nextExpanded != expanded)
            {
                SessionState.SetBool(expandedKey, nextExpanded);
            }

            if (!nextExpanded) return;

            foreach (var definition in definitions)
            {
                DrawModuleEntry(definition);
            }

            DrawAddModuleButton(definitions);

            EditorGUILayout.Space(4);
        }

        void DrawModuleEntry(FluxParticleInspectorModuleMetadata definition)
        {
            var index = FindModuleIndex(definition.ModuleType);
            if (index < 0) return;

            var type = definition.ModuleType;

            if (type == typeof(FluxParticleColorOverLifetimeModule))
            {
                DrawColorOverLifetime();
                return;
            }

            if (type == typeof(FluxParticleSizeOverLifetimeModule))
            {
                DrawSizeOverLifetime();
                return;
            }

            if (type == typeof(FluxParticleRotationOverLifetimeModule))
            {
                DrawRotationOverLifetime();
                return;
            }

            var module = _modules.GetArrayElementAtIndex(index);
            var expanded = SessionState.GetBool(definition.ExpandedKey, false);

            DrawModule(
                L10n.Tr(definition.DisplayName),
                module,
                expanded,
                definition.ExpandedKey,
                () => ShowRemoveModuleMenu(type));
        }

        void DrawAddModuleButton(
            System.Collections.Generic.IReadOnlyList<FluxParticleInspectorModuleMetadata> definitions)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var buttonRect = GUILayoutUtility.GetRect(
                140,
                EditorGUIUtility.singleLineHeight,
                GUILayout.Width(140));

            if (GUI.Button(buttonRect, L10n.Tr("Add Module")))
            {
                ShowAddModuleMenu(definitions, buttonRect);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void ShowAddModuleMenu(
            System.Collections.Generic.IReadOnlyList<FluxParticleInspectorModuleMetadata> definitions,
            Rect buttonRect)
        {
            var menu = new GenericMenu();
            var availableCount = 0;

            foreach (var definition in definitions)
            {
                if (FindModuleIndex(definition.ModuleType) >= 0) continue;

                var selectedDefinition = definition;
                var label = definition.IsCustom
                    ? "Custom/" + definition.DisplayName
                    : L10n.Tr(definition.DisplayName);

                menu.AddItem(
                    new GUIContent(label),
                    false,
                    () => AddModule(selectedDefinition));

                availableCount++;
            }

            if (availableCount == 0)
            {
                menu.AddDisabledItem(new GUIContent(L10n.Tr("All modules added")));
            }

            menu.DropDown(buttonRect);
        }

        void ShowRemoveModuleMenu(Type moduleType)
        {
            var menu = new GenericMenu();

            menu.AddItem(
                new GUIContent(L10n.Tr("Remove Module")),
                false,
                () => RemoveModule(moduleType));

            menu.ShowAsContext();
        }

        int FindModuleIndex(Type moduleType)
        {
            if (_modules == null) return -1;

            for (var i = 0; i < _modules.arraySize; i++)
            {
                var module = _modules.GetArrayElementAtIndex(i);
                if (module.managedReferenceValue == null) continue;

                if (module.managedReferenceValue.GetType() == moduleType) return i;
            }

            return -1;
        }

        void AddModule(FluxParticleInspectorModuleMetadata definition)
        {
            if (_authoringObject == null) return;

            _authoringObject.Update();

            if (FindModuleIndex(definition.ModuleType) >= 0) return;

            var index = _modules.arraySize;
            _modules.arraySize++;

            var module = _modules.GetArrayElementAtIndex(index);
            module.managedReferenceValue = Activator.CreateInstance(definition.ModuleType);

            _authoringObject.ApplyModifiedProperties();
            _preview?.OnAuthoringChanged();

            SessionState.SetBool(definition.ExpandedKey, true);
            SetModuleExpanded(definition.ModuleType, true);

            Repaint();
        }

        void RemoveModule(Type moduleType)
        {
            if (_authoringObject == null) return;

            _authoringObject.Update();

            var index = FindModuleIndex(moduleType);
            if (index < 0) return;

            _modules.DeleteArrayElementAtIndex(index);
            _authoringObject.ApplyModifiedProperties();
            _preview?.OnAuthoringChanged();

            Repaint();
        }

        void SetModuleExpanded(Type moduleType, bool expanded)
        {
            if (moduleType == typeof(FluxParticleColorOverLifetimeModule))
            {
                _colorOverLifetimeExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleSizeOverLifetimeModule))
            {
                _sizeOverLifetimeExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleRotationOverLifetimeModule))
            {
                _rotationOverLifetimeExpanded = expanded;
            }
        }
    }
}