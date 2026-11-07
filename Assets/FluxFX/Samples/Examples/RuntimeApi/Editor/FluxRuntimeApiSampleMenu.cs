using UnityEditor;
using UnityEditor.SceneManagement;

namespace TpLab.Flux.FX.Samples.RuntimeApi.Editor
{
    /// <summary>
    /// インポート先にあるRuntime APIサンプルシーンを開きます。
    /// </summary>
    public static class FluxRuntimeApiSampleMenu
    {
        [MenuItem("Tools/FluxFX/Samples/Open Runtime API Playground")]
        static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var path = AssetDatabase.GUIDToAssetPath("492aa7fbcf23e00459e43813f5f25b0a");
            EditorSceneManager.OpenScene(path);
        }
    }
}
