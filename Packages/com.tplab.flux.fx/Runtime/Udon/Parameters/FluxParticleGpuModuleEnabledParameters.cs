using JetBrains.Annotations;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    [PublicAPI]
    public static class FluxParticleGpuModuleEnabledParameters
    {
        const int StoreIndex = 0;
        const int EnabledParameterIndex = 1;

        [PublicAPI]
        public static bool GetEnabled(this FluxParticleForceParameters parameters)
        {
            return ReadEnabled(parameters);
        }

        [PublicAPI]
        public static void SetEnabled(this FluxParticleForceParameters parameters, bool enabled)
        {
            WriteEnabled(parameters, enabled);
        }

        [PublicAPI]
        public static bool GetEnabled(this FluxParticleNoiseParameters parameters)
        {
            return ReadEnabled(parameters);
        }

        [PublicAPI]
        public static void SetEnabled(this FluxParticleNoiseParameters parameters, bool enabled)
        {
            WriteEnabled(parameters, enabled);
        }

        [PublicAPI]
        public static bool GetEnabled(this FluxParticleVortexParameters parameters)
        {
            return ReadEnabled(parameters);
        }

        [PublicAPI]
        public static void SetEnabled(this FluxParticleVortexParameters parameters, bool enabled)
        {
            WriteEnabled(parameters, enabled);
        }

        [PublicAPI]
        public static bool GetEnabled(this FluxParticleDragParameters parameters)
        {
            return ReadEnabled(parameters);
        }

        [PublicAPI]
        public static void SetEnabled(this FluxParticleDragParameters parameters, bool enabled)
        {
            WriteEnabled(parameters, enabled);
        }

        [PublicAPI]
        public static bool GetEnabled(this FluxParticleLimitVelocityParameters parameters)
        {
            return ReadEnabled(parameters);
        }

        [PublicAPI]
        public static void SetEnabled(this FluxParticleLimitVelocityParameters parameters, bool enabled)
        {
            WriteEnabled(parameters, enabled);
        }

        [PublicAPI]
        public static bool ReadEnabled(DataList parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[StoreIndex].Reference;
            var name = parameters[EnabledParameterIndex].String;

            return store.GetFloat(name) > 0.5f;
        }

        [PublicAPI]
        public static void WriteEnabled(DataList parameters, bool enabled)
        {
            var store = (FluxParticleGpuParameterStore)parameters[StoreIndex].Reference;
            var name = parameters[EnabledParameterIndex].String;

            store.SetFloat(name, enabled ? 1.0f : 0.0f);
        }
    }
}