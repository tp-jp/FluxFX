using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleGpuParameterStoreProbe : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleGpuParameterStore store;

        [SerializeField]
        FluxKernel kernel;

        void Start()
        {
            store.Initialize(kernel);

            if (!store.RegisterFloat("_Drag", 0.5f))
            {
                Debug.LogError("GPU Parameter Store: RegisterFloat failed.");
                return;
            }

            if (!store.RegisterVector("_Gravity", new Vector4(0, -9.81f, 0, 0)))
            {
                Debug.LogError("GPU Parameter Store: RegisterVector failed.");
                return;
            }

            if (store.Count != 2 || store.PendingCount != 2)
            {
                Debug.LogError("GPU Parameter Store: Initial state failed.");
                return;
            }

            if (store.Flush() != 2 || store.PendingCount != 0)
            {
                Debug.LogError("GPU Parameter Store: Initial Flush failed.");
                return;
            }

            store.SetFloat("_Drag", 0.5f);

            if (store.PendingCount != 0)
            {
                Debug.LogError("GPU Parameter Store: Unchanged value was marked dirty.");
                return;
            }

            store.SetFloat("_Drag", 0.6f);
            store.SetFloat("_Drag", 0.7f);
            store.SetFloat("_Drag", 0.8f);

            if (store.PendingCount != 1 || store.GetFloat("_Drag") != 0.8f)
            {
                Debug.LogError("GPU Parameter Store: Dirty aggregation failed.");
                return;
            }

            if (store.Flush() != 1 || store.Flush() != 0)
            {
                Debug.LogError("GPU Parameter Store: Flush aggregation failed.");
                return;
            }

            store.SetVector("_Gravity", new Vector4(0, -5, 0, 0));

            if (store.PendingCount != 1 || store.GetVector("_Gravity").y != -5.0f)
            {
                Debug.LogError("GPU Parameter Store: Vector update failed.");
                return;
            }

            if (store.Flush() != 1 || store.PendingCount != 0)
            {
                Debug.LogError("GPU Parameter Store: Vector Flush failed.");
                return;
            }

            Debug.Log("FluxFX GPU Parameter Store: PASS");
        }
    }
}