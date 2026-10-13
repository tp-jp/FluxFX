using System;
using System.IO;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Runtime用Prefabの固定構造と可変Page/StepテンプレートをEditorで一度だけ構築します。
    /// SceneFlowではUdonSharpBehaviourの追加を行いません。
    /// </summary>
    public static class FluxParticleProgramPrefabSetup
    {
        const string MainPath = "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleSystem.prefab";
        const string TemplateFolder = "Assets/FluxFX/Generated/Prefabs";
        const string PagePath = TemplateFolder + "/FluxParticlePage.prefab";
        const string StepPath = TemplateFolder + "/FluxParticleStep.prefab";

        [MenuItem("Tools/FluxFX/Setup Program Backend Prefabs")]
        static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MainPath) == null)
            {
                Debug.LogError("[FluxFX] FluxParticleSystem prefab was not found.");
                return;
            }

            Directory.CreateDirectory(TemplateFolder);
            var page = new GameObject("Page");
            var step = new GameObject("Step");
            var main = PrefabUtility.LoadPrefabContents(MainPath);
            try
            {
                RequireUdon<FluxBuffer>(page.transform, "BufferA");
                RequireUdon<FluxBuffer>(page.transform, "BufferB");
                RequireUdon<FluxKernel>(page.transform, "ClearKernel");
                if (step.GetComponent<FluxKernel>() == null) step.AddUdonSharpComponent<FluxKernel>();

                var executor = main.transform.Find("Executor");
                if (executor == null)
                {
                    executor = new GameObject("Executor").transform;
                    executor.SetParent(main.transform, false);
                }
                if (executor.GetComponent<FluxParticleExecutor>() == null)
                {
                    executor.gameObject.AddUdonSharpComponent<FluxParticleExecutor>();
                }
                var spawn = executor.Find("Spawn");
                if (spawn == null)
                {
                    spawn = new GameObject("Spawn").transform;
                    spawn.SetParent(executor, false);
                }
                RequireUdon<FluxKernel>(spawn, "MaskKernel");
                RequireUdon<FluxBuffer>(spawn, "MaskBuffer");
                RequireUdon<FluxKernel>(spawn, "KeyKernel");
                RequireUdon<FluxBuffer>(spawn, "KeyBuffer");

                // AddUdonSharpComponent queues backing initialization. Save after its pending callbacks.
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        PrefabUtility.SaveAsPrefabAsset(page, PagePath);
                        PrefabUtility.SaveAsPrefabAsset(step, StepPath);
                        PrefabUtility.SaveAsPrefabAsset(main, MainPath);
                        AssetDatabase.SaveAssets();
                        Debug.Log("[FluxFX] Program Backend prefab setup completed.");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                    finally
                    {
                        Object.DestroyImmediate(page);
                        Object.DestroyImmediate(step);
                        PrefabUtility.UnloadPrefabContents(main);
                    }
                };
            }
            catch
            {
                Object.DestroyImmediate(page);
                Object.DestroyImmediate(step);
                PrefabUtility.UnloadPrefabContents(main);
                throw;
            }
        }

        static T RequireUdon<T>(Transform parent, string name) where T : UdonSharp.UdonSharpBehaviour
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }
            var component = child.GetComponent<T>();
            return component == null ? child.gameObject.AddUdonSharpComponent<T>() : component;
        }
    }
}
