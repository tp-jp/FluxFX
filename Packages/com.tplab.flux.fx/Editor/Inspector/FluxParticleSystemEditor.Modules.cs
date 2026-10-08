using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;
using L10n = TpLab.Flux.FX.Editor.Localization.L10n;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        bool DrawOptionalModule<T>(string label, bool expanded, string sessionStateKey)
            where T : FluxParticleModule, new()
        {
            var moduleIndex = FindModuleIndex<T>();

            if (moduleIndex >= 0)
            {
                var module = _modules.GetArrayElementAtIndex(moduleIndex);

                expanded = DrawModule(
                    label,
                    module,
                    expanded,
                    sessionStateKey);
            }

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(moduleIndex >= 0))
            {
                if (GUILayout.Button(L10n.Tr("Add Module"), GUILayout.Width(120)))
                {
                    if (AddModule<T>())
                    {
                        expanded = true;
                        SessionState.SetBool(sessionStateKey, true);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(moduleIndex < 0))
            {
                if (GUILayout.Button(L10n.Tr("Remove Module"), GUILayout.Width(120)))
                {
                    RemoveModule(moduleIndex);
                }
            }

            EditorGUILayout.EndHorizontal();

            return expanded;
        }

        int FindModuleIndex<T>() where T : FluxParticleModule
        {
            if (_modules == null) return -1;

            for (var i = 0; i < _modules.arraySize; i++)
            {
                var module = _modules.GetArrayElementAtIndex(i);

                if (module.managedReferenceValue is T)
                {
                    return i;
                }
            }

            return -1;
        }

        bool AddModule<T>() where T : FluxParticleModule, new()
        {
            if (_modules == null || FindModuleIndex<T>() >= 0) return false;

            var index = _modules.arraySize;
            _modules.arraySize++;

            var module = _modules.GetArrayElementAtIndex(index);
            module.managedReferenceValue = new T();

            return true;
        }

        void RemoveModule(int index)
        {
            if (_modules == null || index < 0 || index >= _modules.arraySize) return;

            _modules.DeleteArrayElementAtIndex(index);
        }
    }
}