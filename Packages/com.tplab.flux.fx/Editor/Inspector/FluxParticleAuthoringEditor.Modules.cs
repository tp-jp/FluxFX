using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Editor.Localization;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleAuthoringEditor
    {
        readonly List<FluxParticleModule> _indexedModules = new List<FluxParticleModule>();
        readonly List<SerializedProperty> _moduleProperties = new List<SerializedProperty>();
        readonly Dictionary<Type, int> _moduleIndices = new Dictionary<Type, int>();

        // SerializedPropertyの配列走査はPlay中に高コストになるため、構成が変わるまで再利用する。
        void RefreshModuleIndex()
        {
            var modules = ((FluxParticleAuthoring)target).Modules;
            var unchanged = modules.Count == _indexedModules.Count;
            for (var i = 0; unchanged && i < modules.Count; i++)
            {
                unchanged = ReferenceEquals(modules[i], _indexedModules[i]);
            }
            if (unchanged) return;

            DisposeModuleIndex();
            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                _indexedModules.Add(module);
                _moduleProperties.Add(null);
                if (module != null && !_moduleIndices.ContainsKey(module.GetType()))
                {
                    _moduleIndices.Add(module.GetType(), i);
                }
            }
        }

        SerializedProperty GetModuleProperty(int index)
        {
            RefreshModuleIndex();
            return _moduleProperties[index] ?? (_moduleProperties[index] = _modules.GetArrayElementAtIndex(index));
        }

        void DisposeModuleIndex()
        {
            foreach (var property in _moduleProperties) property?.Dispose();
            _moduleProperties.Clear();
            _indexedModules.Clear();
            _moduleIndices.Clear();
        }

        void DrawModules()
        {
            _modulesExpanded = DrawSectionHeader(L10n.Tr("MODULES"), _modulesExpanded, ModulesExpandedKey);
            if (!_modulesExpanded) return;

            EditorGUILayout.Space(2);

            DrawUnsupportedModules();
            DrawModuleStage(FluxParticleExecutionStage.Spawn, "Spawn", SessionStatePrefix + "SpawnModulesExpanded");
            DrawModuleStage(FluxParticleExecutionStage.SpawnAttributes, "Spawn Attributes", SessionStatePrefix + "SpawnAttributeModulesExpanded");
            DrawModuleStage(FluxParticleExecutionStage.VelocityUpdate, "Velocity Update", VelocityUpdateExpandedKey);
            DrawModuleStage(FluxParticleExecutionStage.PositionUpdate, "Position Update", PositionUpdateExpandedKey);
            DrawModuleStage(FluxParticleExecutionStage.Rendering, "Rendering", RenderingModulesExpandedKey);
            foreach (var stage in _moduleRegistry.GetAll().Select(metadata => metadata.Stage).Distinct())
            {
                if (stage == FluxParticleExecutionStage.Spawn || stage == FluxParticleExecutionStage.SpawnAttributes ||
                    stage == FluxParticleExecutionStage.VelocityUpdate || stage == FluxParticleExecutionStage.PositionUpdate ||
                    stage == FluxParticleExecutionStage.Rendering) continue;
                DrawModuleStage(stage, stage.ToString(), SessionStatePrefix + "ModuleStage." + stage);
            }
        }

        void DrawUnsupportedModules()
        {
            for (var i = 0; i < _modules.arraySize; i++)
            {
                var property = GetModuleProperty(i);
                var value = property.managedReferenceValue;
                if (value != null && _moduleRegistry.TryGet(value.GetType(), out _)) continue;

                var index = i;
                var label = value == null ? L10n.Tr("Missing Module") : ObjectNames.NicifyVariableName(value.GetType().Name);
                EditorGUILayout.HelpBox(label + ": " + L10n.Tr("This module is not supported by the current backend. Disabling it does not allow building. Remove it or provide a current Module Definition."),
                    MessageType.Warning);
                if (value != null)
                {
                    DrawModule(label, property, SessionState.GetBool(SessionStatePrefix + "Unsupported." + index, false),
                        SessionStatePrefix + "Unsupported." + index, () => RemoveModuleAt(index));
                }
                else if (GUILayout.Button(L10n.Tr("Remove Module")))
                {
                    RemoveModuleAt(index);
                    GUIUtility.ExitGUI();
                }
            }
        }

        void RemoveModuleAt(int index)
        {
            if (!CanEditAuthoring || _authoringObject == null) return;
            _authoringObject.Update();
            if (index < 0 || index >= _modules.arraySize) return;
            _modules.DeleteArrayElementAtIndex(index);
            _authoringObject.ApplyModifiedProperties();
            Repaint();
        }
        void DrawModuleStage(FluxParticleExecutionStage stage, string label, string expandedKey)
        {
            var definitions = _moduleRegistry.GetModules(stage);
            if (definitions.Count == 0) return;

            var expanded = SessionState.GetBool(expandedKey, true);
            var previousEnabled = GUI.enabled;
            bool nextExpanded;
            try
            {
                GUI.enabled = true;
                nextExpanded = EditorGUILayout.Foldout(expanded, L10n.Tr(label), true, EditorStyles.foldoutHeader);
            }
            finally
            {
                GUI.enabled = previousEnabled;
            }

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

            var module = GetModuleProperty(index);
            var expanded = SessionState.GetBool(definition.ExpandedKey, false);

            DrawModule(
                definition.Translate(definition.DisplayName),
                module,
                expanded,
                definition.ExpandedKey,
                () => ShowRemoveModuleMenu(type),
                definition);

            if (type == typeof(FluxParticleAudioInputModule) && !FluxParticleAudioInputDefinition.IsAudioLinkInstalled)
            {
                EditorGUILayout.HelpBox(L10n.Tr("AudioLink is not installed. Audio Input outputs remain zero. Install the AudioLink package to use audio input."),
                    MessageType.Warning);
            }
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
                    ? L10n.Tr("Custom") + "/" + definition.Translate(definition.DisplayName)
                    : definition.Translate(definition.DisplayName);

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
            RefreshModuleIndex();
            return _moduleIndices.TryGetValue(moduleType, out var index) ? index : -1;
        }

        void AddModule(FluxParticleInspectorModuleMetadata definition)
        {
            if (!CanEditAuthoring || _authoringObject == null) return;

            _authoringObject.Update();

            if (FindModuleIndex(definition.ModuleType) >= 0) return;

            var index = _modules.arraySize;
            _modules.arraySize++;

            var module = _modules.GetArrayElementAtIndex(index);
            module.managedReferenceValue = Activator.CreateInstance(definition.ModuleType);

            _authoringObject.ApplyModifiedProperties();

            SessionState.SetBool(definition.ExpandedKey, true);
            SetModuleExpanded(definition.ModuleType, true);

            Repaint();
        }

        void RemoveModule(Type moduleType)
        {
            if (!CanEditAuthoring || _authoringObject == null) return;

            _authoringObject.Update();

            var index = FindModuleIndex(moduleType);
            if (index < 0) return;

            _modules.DeleteArrayElementAtIndex(index);
            _authoringObject.ApplyModifiedProperties();

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
