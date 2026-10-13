using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    [PublicAPI]
    public class FluxParticleModuleParameters : DataList
    {
        protected const int StoreIndex = 0;
        protected const int EnabledParameterIndex = 1;

        [PublicAPI]
        public static DataList Create(DataList binding)
        {
            if (binding == null) return null;

            var data = new DataToken[2];
            data[StoreIndex] = binding[StoreIndex];
            data[EnabledParameterIndex] = binding[EnabledParameterIndex];

            return new DataList(data);
        }
    }

    [PublicAPI]
    public static class FluxParticleModuleParametersExtensions
    {
        [PublicAPI]
        public static bool GetEnabled(this FluxParticleModuleParameters parameters)
        {
            var store = GetStore(parameters);
            var name = parameters[1].String;

            return store.GetFloat(name) > 0.5f;
        }

        [PublicAPI]
        public static void SetEnabled(this FluxParticleModuleParameters parameters, bool enabled)
        {
            var store = GetStore(parameters);
            var name = parameters[1].String;

            store.SetFloat(name, enabled ? 1.0f : 0.0f);
        }

        [PublicAPI]
        public static float GetFloat(this FluxParticleModuleParameters parameters, string name)
        {
            return GetStore(parameters).GetFloat(name);
        }

        [PublicAPI]
        public static bool SetFloat(this FluxParticleModuleParameters parameters, string name, float value)
        {
            return GetStore(parameters).SetFloat(name, value);
        }

        [PublicAPI]
        public static Vector4 GetVector(this FluxParticleModuleParameters parameters, string name)
        {
            return GetStore(parameters).GetVector(name);
        }

        [PublicAPI]
        public static bool SetVector(this FluxParticleModuleParameters parameters, string name, Vector4 value)
        {
            return GetStore(parameters).SetVector(name, value);
        }

        static FluxParticleGpuParameterStore GetStore(FluxParticleModuleParameters parameters)
        {
            return (FluxParticleGpuParameterStore)parameters[0].Reference;
        }
    }
}