using TpLab.Flux.FX.Scripts;
using UnityEditor;

namespace TpLab.Flux.FX.CustomModules.Editor
{
    public static class CustomLiftTestMenu
    {
        [MenuItem("CONTEXT/FluxParticleAuthoring/Add Custom Lift Module")]
        static void Add(MenuCommand command)
        {
            var authoring = (FluxParticleAuthoring)command.context;
            var serializedObject = new SerializedObject(authoring);
            var modules = serializedObject.FindProperty("modules");

            for (var i = 0; i < modules.arraySize; i++)
            {
                if (modules.GetArrayElementAtIndex(i).managedReferenceValue is CustomLiftModule)
                    return;
            }

            Undo.RecordObject(authoring, "Add Custom Lift Module");

            var index = modules.arraySize;
            modules.arraySize++;

            modules.GetArrayElementAtIndex(index).managedReferenceValue = new CustomLiftModule();
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(authoring);
        }
    }
}