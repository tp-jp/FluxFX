using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor
{
    public static class FluxParticleModuleTestMenu
    {
        [MenuItem("Tools/FluxFX/Test/Add Gravity Module")]
        static void AddGravityModule()
        {
            var authoring = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<FluxParticleAuthoring>()
                : null;

            if (authoring == null)
            {
                Debug.LogWarning("[FluxFX] Select a GameObject with FluxParticleAuthoring.");
                return;
            }

            var serializedObject = new SerializedObject(authoring);
            serializedObject.Update();

            var modules = serializedObject.FindProperty("modules");

            for (var i = 0; i < modules.arraySize; i++)
            {
                var module = modules.GetArrayElementAtIndex(i);

                if (module.managedReferenceValue is FluxParticleGravityModule)
                {
                    Debug.LogWarning("[FluxFX] Gravity Module already exists.");
                    return;
                }
            }

            var index = modules.arraySize;
            modules.arraySize++;

            var element = modules.GetArrayElementAtIndex(index);
            element.managedReferenceValue = new FluxParticleGravityModule();

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(authoring);

            Debug.Log("[FluxFX] Gravity Module added.", authoring);
        }
    }
}