
using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        struct ModuleDefinition
        {
            public readonly Type Type;
            public readonly string Label;
            public readonly string ExpandedKey;

            public ModuleDefinition(Type type, string label, string expandedKey)
            {
                Type = type;
                Label = label;
                ExpandedKey = expandedKey;
            }
        }

        static readonly ModuleDefinition[] VelocityModuleDefinitions =
        {
            new ModuleDefinition(typeof(FluxParticleVelocityOverLifetimeModule), "Velocity over Lifetime", VelocityOverLifetimeExpandedKey),
            new ModuleDefinition(typeof(FluxParticleGravityModule), "Gravity", GravityExpandedKey),
            new ModuleDefinition(typeof(FluxParticleForceModule), "Force", ForceExpandedKey),
            new ModuleDefinition(typeof(FluxParticleDragModule), "Drag", DragExpandedKey),
            new ModuleDefinition(typeof(FluxParticleNoiseModule), "Noise", NoiseExpandedKey),
            new ModuleDefinition(typeof(FluxParticleVortexModule), "Vortex", VortexExpandedKey),
            new ModuleDefinition(typeof(FluxParticleLimitVelocityModule), "Limit Velocity", LimitVelocityExpandedKey)
        };

        static readonly ModuleDefinition[] RendererModuleDefinitions =
        {
            new ModuleDefinition(typeof(FluxParticleColorOverLifetimeModule), "Color over Lifetime", ColorOverLifetimeExpandedKey),
            new ModuleDefinition(typeof(FluxParticleSizeOverLifetimeModule), "Size over Lifetime", SizeOverLifetimeExpandedKey),
            new ModuleDefinition(typeof(FluxParticleRotationOverLifetimeModule), "Rotation over Lifetime", RotationOverLifetimeExpandedKey),
            new ModuleDefinition(typeof(FluxParticleTextureSheetAnimationModule), "Texture Sheet Animation", TextureSheetAnimationExpandedKey)
        };

        bool DrawOptionalModule<T>(string label, bool expanded, string sessionStateKey)
            where T : FluxParticleModule
        {
            var moduleIndex = FindModuleIndex(typeof(T));
            if (moduleIndex < 0) return expanded;

            var module = _modules.GetArrayElementAtIndex(moduleIndex);

            return DrawModule(
                label,
                module,
                expanded,
                sessionStateKey,
                () => ShowRemoveModuleMenu(typeof(T)));
        }

        void DrawAddModuleButton(ModuleDefinition[] definitions)
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

        void ShowAddModuleMenu(ModuleDefinition[] definitions, Rect buttonRect)
        {
            var menu = new GenericMenu();
            var availableCount = 0;

            foreach (var definition in definitions)
            {
                if (FindModuleIndex(definition.Type) >= 0) continue;

                var selectedDefinition = definition;

                menu.AddItem(
                    new GUIContent(L10n.Tr(selectedDefinition.Label)),
                    false,
                    () => AddModule(selectedDefinition));

                availableCount++;
            }

            if (ReferenceEquals(definitions, VelocityModuleDefinitions))
            {
                foreach (var definition in GetCustomVelocityModuleDefinitions())
                {
                    if (FindModuleIndex(definition.Type) >= 0) continue;

                    var selectedDefinition = definition;

                    menu.AddItem(
                        new GUIContent("Custom/" + selectedDefinition.Label),
                        false,
                        () => AddModule(selectedDefinition));

                    availableCount++;
                }
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

                if (module.managedReferenceValue.GetType() == moduleType)
                {
                    return i;
                }
            }

            return -1;
        }

        void AddModule(ModuleDefinition definition)
        {
            if (_authoringObject == null) return;

            _authoringObject.Update();

            if (FindModuleIndex(definition.Type) >= 0) return;

            var index = _modules.arraySize;
            _modules.arraySize++;

            var module = _modules.GetArrayElementAtIndex(index);
            module.managedReferenceValue = Activator.CreateInstance(definition.Type);

            _authoringObject.ApplyModifiedProperties();

            SessionState.SetBool(definition.ExpandedKey, true);
            SetModuleExpanded(definition.Type, true);

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

            Repaint();
        }

        void SetModuleExpanded(Type moduleType, bool expanded)
        {
            if (moduleType == typeof(FluxParticleVelocityOverLifetimeModule))
            {
                _velocityOverLifetimeExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleGravityModule))
            {
                _gravityExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleForceModule))
            {
                _forceExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleDragModule))
            {
                _dragExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleNoiseModule))
            {
                _noiseExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleVortexModule))
            {
                _vortexExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleLimitVelocityModule))
            {
                _limitVelocityExpanded = expanded;
            }
            else if (moduleType == typeof(FluxParticleColorOverLifetimeModule))
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
            else if (moduleType == typeof(FluxParticleTextureSheetAnimationModule))
            {
                _textureSheetAnimationExpanded = expanded;
            }
        }
    }
}
