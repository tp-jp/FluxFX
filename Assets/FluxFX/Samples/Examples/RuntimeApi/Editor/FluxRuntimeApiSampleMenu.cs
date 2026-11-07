using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            OpenScene("RuntimeApiPlayground.unity");
        }

        [MenuItem("Tools/FluxFX/Samples/Open Synced Runtime API")]
        static void OpenSynced()
        {
            OpenScene("SyncedRuntimeApi.unity");
        }

        [MenuItem("Tools/FluxFX/Samples/Open Million Particles")]
        static void OpenMillion()
        {
            OpenScene("MillionParticles.unity");
        }

        static void OpenScene(string filename)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var original = AssetDatabase.GUIDToAssetPath("492aa7fbcf23e00459e43813f5f25b0a");
            var path = Path.Combine(Path.GetDirectoryName(original) ?? "", filename).Replace('\\', '/');
            if (!File.Exists(path))
            {
                Debug.LogError("Sample scene is not installed: " + filename);
                return;
            }
            EditorSceneManager.OpenScene(path);
        }
    }
}
