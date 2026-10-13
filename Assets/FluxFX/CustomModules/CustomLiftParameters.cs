using JetBrains.Annotations;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.CustomModules
{
    [PublicAPI]
    public class CustomLiftParameters : FluxParticleModuleParameters
    {
        public static CustomLiftParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (CustomLiftParameters)data;
        }
    }

    [PublicAPI]
    public static class CustomLiftParametersExtensions
    {
        [PublicAPI]
        public static float GetStrength(this CustomLiftParameters parameters)
        {
            return parameters.GetFloat("_CustomLiftStrength");
        }

        [PublicAPI]
        public static void SetStrength(this CustomLiftParameters parameters, float strength)
        {
            parameters.SetFloat("_CustomLiftStrength", Mathf.Max(0, strength));
        }

        [PublicAPI]
        public static Vector3 GetDirection(this CustomLiftParameters parameters)
        {
            var direction = parameters.GetVector("_CustomLiftDirection");

            return new Vector3(direction.x, direction.y, direction.z);
        }

        [PublicAPI]
        public static void SetDirection(this CustomLiftParameters parameters, Vector3 direction)
        {
            parameters.SetVector("_CustomLiftDirection", new Vector4(direction.x, direction.y, direction.z, 0));
        }
    }

    // [PublicAPI]
    // public static class CustomLiftParticleSystemExtensions
    // {
    //     [PublicAPI]
    //     public static CustomLiftParameters GetCustomLift(this FluxParticleSystem particleSystem)
    //     {
    //         return CustomLiftParameters.New(particleSystem.TryGetGpuModuleBinding("custom.fluxfx.lift"));
    //     }
    // }
}