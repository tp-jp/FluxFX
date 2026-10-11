using JetBrains.Annotations;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.CustomModules
{
    enum CustomLiftParameter
    {
        Store,
        EnabledParameter,

        Count
    }

    [PublicAPI]
    public class CustomLiftParameters : DataList
    {
        public static CustomLiftParameters New(DataList binding)
        {
            if (binding == null) return null;

            var data = new DataToken[(int)CustomLiftParameter.Count];
            data[(int)CustomLiftParameter.Store] = binding[(int)CustomLiftParameter.Store];
            data[(int)CustomLiftParameter.EnabledParameter] = binding[(int)CustomLiftParameter.EnabledParameter];

            return (CustomLiftParameters)new DataList(data);
        }
    }

    [PublicAPI]
    public static class CustomLiftParametersExtensions
    {
        [PublicAPI]
        public static float GetStrength(this CustomLiftParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)CustomLiftParameter.Store].Reference;

            return store.GetFloat("_CustomLiftStrength");
        }

        [PublicAPI]
        public static void SetStrength(this CustomLiftParameters parameters, float strength)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)CustomLiftParameter.Store].Reference;

            store.SetFloat("_CustomLiftStrength", Mathf.Max(0, strength));
        }

        [PublicAPI]
        public static Vector3 GetDirection(this CustomLiftParameters parameters)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)CustomLiftParameter.Store].Reference;
            var direction = store.GetVector("_CustomLiftDirection");

            return new Vector3(direction.x, direction.y, direction.z);
        }

        [PublicAPI]
        public static void SetDirection(this CustomLiftParameters parameters, Vector3 direction)
        {
            var store = (FluxParticleGpuParameterStore)parameters[(int)CustomLiftParameter.Store].Reference;

            store.SetVector("_CustomLiftDirection", new Vector4(direction.x, direction.y, direction.z, 0));
        }

        [PublicAPI]
        public static bool GetEnabled(this CustomLiftParameters parameters)
        {
            return FluxParticleGpuModuleEnabledParameters.ReadEnabled(parameters);
        }

        [PublicAPI]
        public static void SetEnabled(this CustomLiftParameters parameters, bool enabled)
        {
            FluxParticleGpuModuleEnabledParameters.WriteEnabled(parameters, enabled);
        }
    }

    [PublicAPI]
    public static class CustomLiftParticleSystemExtensions
    {
        [PublicAPI]
        public static CustomLiftParameters GetCustomLift(this FluxParticleSystem particleSystem)
        {
            var binding = particleSystem.GetGpuModuleBinding("custom.fluxfx.lift");

            return CustomLiftParameters.New(binding);
        }
    }
}