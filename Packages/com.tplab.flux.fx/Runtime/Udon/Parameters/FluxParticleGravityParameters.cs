using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    [PublicAPI]
    public class FluxParticleGravityParameters : FluxParticleModuleParameters
    {
        internal static FluxParticleGravityParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (FluxParticleGravityParameters)data;
        }
    }

    [PublicAPI]
    public static class FluxParticleGravityParametersExtensions
    {
        [PublicAPI]
        public static Vector3 GetGravity(this FluxParticleGravityParameters parameters)
        {
            var gravity = parameters.GetVector("_Gravity");

            return new Vector3(gravity.x, gravity.y, gravity.z);
        }

        [PublicAPI]
        public static void SetGravity(this FluxParticleGravityParameters parameters, Vector3 gravity)
        {
            parameters.SetVector("_Gravity", new Vector4(gravity.x, gravity.y, gravity.z, 0));
        }
    }
}