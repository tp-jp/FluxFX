using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    /// <summary>
    /// SceneFlowがPrefab Assetから複製したUdonSharpBehaviourのRuntime実行を検証します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxPrefabAssetCloneProbe : UdonSharpBehaviour
    {
        [SerializeField]
        FluxKernel kernel;

        [SerializeField]
        FluxBuffer buffer;

        void Start()
        {
            if (kernel == null || buffer == null)
            {
                Debug.LogError("[FluxFX] Prefab Asset Clone Runtime FAIL: Udon references are missing.");
                return;
            }

            if (buffer.Count != 4 || buffer.Texture == null)
            {
                Debug.LogError("[FluxFX] Prefab Asset Clone Runtime FAIL: FluxBuffer initialization failed.");
                return;
            }

            var texture = buffer.Texture;
            kernel.SetVector("_FluxStateSize", new Vector4(texture.width, texture.height, buffer.Count, 0));
            kernel.SetFloat("_FluxSpawnFirst", 0);
            kernel.SetFloat("_FluxSpawnCount", buffer.Count);
            kernel.Dispatch(buffer);

            Debug.Log("[FluxFX] Prefab Asset Clone Runtime PASS: FluxBuffer and FluxKernel executed from a SceneFlow-instantiated Prefab Asset.");
        }
    }
}