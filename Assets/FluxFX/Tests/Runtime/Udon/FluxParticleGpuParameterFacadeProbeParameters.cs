using TpLab.Flux.FX.Udon;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Tests.Udon
{
    public class FluxParticleGpuParameterFacadeProbeParameters : DataList
    {
        public static FluxParticleGpuParameterFacadeProbeParameters New(FluxParticleGpuParameterStore store)
        {
            var data = new DataToken[1];
            data[0] = new DataToken(store);
            return (FluxParticleGpuParameterFacadeProbeParameters)new DataList(data);
        }
    }

    public static class FluxParticleGpuParameterFacadeProbeParametersExtensions
    {
        public static float GetDrag(this FluxParticleGpuParameterFacadeProbeParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[0].Reference;
            return store.GetFloat("_Drag");
        }

        public static bool SetDrag(this FluxParticleGpuParameterFacadeProbeParameters parameters, float drag)
        {
            var store = (FluxParticleGpuParameterStore)parameters[0].Reference;
            return store.SetFloat("_Drag", drag);
        }
    }
}