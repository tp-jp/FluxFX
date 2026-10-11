using TpLab.Flux.FX.Udon;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleGpuParameterFacadeProbe : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleGpuParameterStore velocityParameterStore;

        void Start()
        {
            SendCustomEventDelayedFrames(nameof(RunProbe), 3);
        }

        public void RunProbe()
        {
            if (velocityParameterStore == null)
            {
                Logger.LogError("GPU Parameter Facade Probe: Store is not assigned.");
                return;
            }

            var parameters = FluxParticleGpuParameterFacadeProbeParameters.New(velocityParameterStore);

            var initialValue = parameters.GetDrag();
            var nextValue = initialValue + 0.25f;

            if (!parameters.SetDrag(nextValue))
            {
                Logger.LogError("GPU Parameter Facade Probe: SetDrag failed.");
                return;
            }

            if (parameters.GetDrag() != nextValue || velocityParameterStore.GetFloat("_Drag") != nextValue)
            {
                Logger.LogError("GPU Parameter Facade Probe: Store value mismatch.");
                return;
            }

            var dirtyCount = velocityParameterStore.PendingCount;

            if (dirtyCount == 0)
            {
                Logger.LogError("GPU Parameter Facade Probe: Parameter was not marked dirty.");
                return;
            }

            if (!parameters.SetDrag(nextValue) || velocityParameterStore.PendingCount != dirtyCount)
            {
                Logger.LogError("GPU Parameter Facade Probe: Unchanged value was marked dirty again.");
                return;
            }

            if (!parameters.SetDrag(initialValue))
            {
                Logger.LogError("GPU Parameter Facade Probe: Failed to restore initial value.");
                return;
            }

            if (parameters.GetDrag() != initialValue)
            {
                Logger.LogError("GPU Parameter Facade Probe: Restored value mismatch.");
                return;
            }

            Logger.Log("GPU Parameter Facade Probe: PASS");
        }
    }
}