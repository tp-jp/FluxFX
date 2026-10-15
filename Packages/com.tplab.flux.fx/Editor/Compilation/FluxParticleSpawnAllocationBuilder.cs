using System;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// RuntimeとPreviewで使用する空きSlot集計のBufferとKernelを構成します。
    /// </summary>
    internal static class FluxParticleSpawnAllocationBuilder
    {
        const string BufferPrefabPath = "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleBuffer.prefab";
        const string KernelPrefabPath = "Packages/com.tplab.flux.fx/Runtime/Prefabs/FluxParticleStep.prefab";

        internal static void ValidateTemplates()
        {
            var buffer = AssetDatabase.LoadAssetAtPath<GameObject>(BufferPrefabPath);
            var kernel = AssetDatabase.LoadAssetAtPath<GameObject>(KernelPrefabPath);
            if (buffer == null || buffer.GetComponent<FluxBuffer>() == null || kernel == null || kernel.GetComponent<FluxKernel>() == null)
            {
                throw new InvalidOperationException("Spawn allocation Buffer/Kernel Prefabs are missing.");
            }
        }

        internal static (FluxBuffer[] Buffers, FluxKernel[] Kernels) Create(Transform parent, int count, Shader[] shaders, bool preview)
        {
            if (count < 1 || count > FluxParticleCapacity.MaximumExactCount) throw new ArgumentOutOfRangeException(nameof(count));
            if (shaders == null || shaders.Length != 4) throw new ArgumentException("Four allocation shaders are required.", nameof(shaders));
            var buffers = new FluxBuffer[FluxParticleSpawnAllocator.GetLevelCount(count)];
            var kernels = new FluxKernel[buffers.Length + 2];
            for (var level = 0; level < buffers.Length; level++)
            {
                buffers[level] = CreateBuffer(parent, "Level " + level, count, preview);
                kernels[level] = CreateKernel(parent, level == 0 ? "Availability" : "Reduction " + level,
                    shaders[level == 0 ? 0 : 1], preview);
                count = (count + 3) / 4;
            }
            kernels[buffers.Length] = CreateKernel(parent, "Ranks", shaders[2], preview);
            kernels[buffers.Length + 1] = CreateKernel(parent, "Keys", shaders[3], preview);
            return (buffers, kernels);
        }

        internal static FluxBuffer CreateBuffer(Transform parent, string name, int count, bool preview)
        {
            var go = CreateObject(parent, name, preview, BufferPrefabPath);
            var buffer = preview ? go.AddComponent<FluxBuffer>() : go.GetComponent<FluxBuffer>();
            if (preview) buffer.hideFlags = HideFlags.HideAndDontSave;
            buffer.SetProgramVariable("initialCount", count);
            return buffer;
        }

        static FluxKernel CreateKernel(Transform parent, string name, Shader shader, bool preview)
        {
            var go = CreateObject(parent, name, preview, KernelPrefabPath);
            var kernel = preview ? go.AddComponent<FluxKernel>() : go.GetComponent<FluxKernel>();
            if (preview) kernel.hideFlags = HideFlags.HideAndDontSave;
            kernel.SetProgramVariable("shader", shader);
            return kernel;
        }

        static GameObject CreateObject(Transform parent, string name, bool preview, string prefabPath)
        {
            var go = preview ? new GameObject(name)
                : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), parent.gameObject.scene);
            go.name = name;
            if (preview) go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
