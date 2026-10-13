using System;
using System.Linq;
using TpLab.Flux.FX.Editor;
using TpLab.Flux.FX.Tests.Udon;
using TpLab.Flux.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Tests.Editor
{
    /// <summary>
    /// Sceneに未配置のPrefab AssetからUdonSharpBehaviourを複製できるか検証するためのEditorセットアップです。
    /// </summary>
    public static class FluxPrefabAssetCloneValidation
    {
        internal const string MarkerName = "__FluxFXPrefabAssetCloneValidation";
        internal const string InstanceName = "__FluxFXPrefabAssetCloneInstance";
        internal const string TemplatePath = "Assets/FluxFX/Tests/PrefabAssetCloneValidation/KernelBufferProbe.prefab";

        const string TemplateFolder = "Assets/FluxFX/Tests/PrefabAssetCloneValidation";
        const string ShaderPath = "Packages/com.tplab.flux.fx/Runtime/Shaders/FluxParticleSpawnMask.shader";

        [MenuItem("Tools/FluxFX/Tests/Prepare Prefab Asset Clone Validation")]
        static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("[FluxFX] Save the active Scene before preparing the Prefab Asset clone test.");
                return;
            }
            if (scene.GetRootGameObjects().Any(root => root.name == MarkerName || root.name == InstanceName))
            {
                Debug.LogError("[FluxFX] Prefab Asset clone test marker or instance already exists. Remove it before preparing again.");
                return;
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null || ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogError("[FluxFX] SpawnMask Shader is not available for Prefab Asset clone validation.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(TemplateFolder))
            {
                AssetDatabase.CreateFolder("Assets/FluxFX/Tests", "PrefabAssetCloneValidation");
            }

            // This staging object only creates a persistent Prefab Asset. It is removed before the Scene is saved.
            // AddUdonSharpComponent is deliberately NOT invoked from any SceneFlow BuildPass.
            var staging = new GameObject("__FluxFXPrefabAssetCloneStaging");
            var kernel = staging.AddUdonSharpComponent<FluxKernel>();
            var buffer = staging.AddUdonSharpComponent<FluxBuffer>();
            var probe = staging.AddUdonSharpComponent<FluxPrefabAssetCloneProbe>();
            kernel.SetProgramVariable("shader", shader);
            buffer.SetProgramVariable("initialCount", 4);
            probe.SetProgramVariable("kernel", kernel);
            probe.SetProgramVariable("buffer", buffer);

            // UdonSharp schedules backing UdonBehaviour updates via delayCall.
            // Our callback runs after the callbacks queued by AddUdonSharpComponent.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    if (staging == null || !scene.IsValid() || !scene.isLoaded)
                    {
                        throw new InvalidOperationException("The staging object or target Scene was removed before Prefab creation.");
                    }

                    var saved = PrefabUtility.SaveAsPrefabAsset(staging, TemplatePath);
                    if (saved == null)
                    {
                        throw new InvalidOperationException("Failed to save the UdonSharp Prefab Asset.");
                    }

                    // Prefab saving may also schedule delayed UdonSharp work. Finish on the next callback.
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            if (staging != null) Object.DestroyImmediate(staging);
                            if (!scene.IsValid() || !scene.isLoaded)
                            {
                                throw new InvalidOperationException("The target Scene was closed before setup completed.");
                            }

                            var marker = new GameObject(MarkerName);
                            if (marker.scene != scene) SceneManager.MoveGameObjectToScene(marker, scene);
                            if (!EditorSceneManager.SaveScene(scene))
                            {
                                throw new InvalidOperationException("Failed to save the test Scene.");
                            }

                            Debug.Log("[FluxFX] Prefab Asset Clone setup PASS: Scene has a marker only, not the UdonSharp template. Enter Play or Build & Test and check both Editor and Runtime PASS logs.");
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError($"[FluxFX] Prefab Asset Clone setup FAIL: {exception}");
                            if (staging != null) Object.DestroyImmediate(staging);
                        }
                    };
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[FluxFX] Prefab Asset Clone setup FAIL: {exception}");
                    if (staging != null) Object.DestroyImmediate(staging);
                }
            };
        }

        internal static bool IsTargetScene(Scene scene)
        {
            return scene.IsValid() && scene.GetRootGameObjects().Any(root => root.name == MarkerName);
        }

        internal static GameObject FindInstance(Scene scene)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == InstanceName);
        }

        internal static void ValidateInstance(GameObject instance, bool checkMaterial)
        {
            var kernel = instance.GetUdonSharpComponent<FluxKernel>();
            var buffer = instance.GetUdonSharpComponent<FluxBuffer>();
            var probe = instance.GetUdonSharpComponent<FluxPrefabAssetCloneProbe>();
            if (kernel == null || buffer == null || probe == null)
            {
                throw new InvalidOperationException("A FluxKernel, FluxBuffer, or runtime Probe proxy is missing.");
            }
            if (instance.GetComponents<UdonBehaviour>().Length < 3)
            {
                throw new InvalidOperationException("The cloned UdonSharp components do not have their backing UdonBehaviours.");
            }
            if (!ReferenceEquals(probe.GetProgramVariable("kernel"), kernel) ||
                !ReferenceEquals(probe.GetProgramVariable("buffer"), buffer))
            {
                throw new InvalidOperationException("The runtime Probe's local UdonSharp references were not preserved.");
            }
            if (checkMaterial && !(kernel.GetProgramVariable("material") is Material))
            {
                throw new InvalidOperationException("FluxBuildPass did not assign a Material to the cloned Kernel.");
            }
        }
    }

    /// <summary>
    /// TemplateをSceneに事前配置せず、SceneFlow実行時にProjectのPrefab Assetから複製します。
    /// </summary>
    public sealed class FluxPrefabAssetCloneBuildPass : PassBase
    {
        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.Before<FluxFXCompileBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            if (!FluxPrefabAssetCloneValidation.IsTargetScene(context.Scene)) return;
            // SceneFlow can run repeatedly for Play / Build; always exercise Asset instantiation again.
            var previous = FluxPrefabAssetCloneValidation.FindInstance(context.Scene);
            if (previous != null) Object.DestroyImmediate(previous);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FluxPrefabAssetCloneValidation.TemplatePath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Prefab Asset clone test template was not found. Run Prepare Prefab Asset Clone Validation first.");
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab, context.Scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("PrefabUtility.InstantiatePrefab did not create an instance in the target Scene.");
            }
            instance.name = FluxPrefabAssetCloneValidation.InstanceName;
            FluxPrefabAssetCloneValidation.ValidateInstance(instance, false);
            Debug.Log("[FluxFX] Prefab Asset Clone BuildPass PASS: cloned UdonSharpBehaviour from a Prefab Asset that was absent from the Scene.");
        }
    }

    /// <summary>
    /// FluxBuildPassの実行後に、複製したKernelへMaterialが割り当てられたことを確認します。
    /// </summary>
    public sealed class FluxPrefabAssetCloneVerifyPass : PassBase
    {
        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.After<FluxFXApplyBuildPass>();
            builder.After<FluxPrefabAssetCloneBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            if (!FluxPrefabAssetCloneValidation.IsTargetScene(context.Scene)) return;
            var instance = FluxPrefabAssetCloneValidation.FindInstance(context.Scene);
            if (instance == null)
            {
                throw new InvalidOperationException("The Prefab Asset clone was not found after FluxBuildPass.");
            }
            FluxPrefabAssetCloneValidation.ValidateInstance(instance, true);
            Debug.Log("[FluxFX] Prefab Asset Clone VerifyPass PASS: Backing UdonBehaviours, references, and FluxKernel Material are valid.");
        }
    }
}
