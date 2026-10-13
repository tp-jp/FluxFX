using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon.Parameters
{
    [PublicAPI]
    public class FluxParticleVelocityOverLifetimeParameters : FluxParticleModuleParameters
    {
        internal static FluxParticleVelocityOverLifetimeParameters New(DataList binding)
        {
            var data = Create(binding);
            if (data == null) return null;

            return (FluxParticleVelocityOverLifetimeParameters)data;
        }
    }

    [PublicAPI]
    public static class FluxParticleVelocityOverLifetimeParametersExtensions
    {
        [PublicAPI]
        public static Vector3 GetStart(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            var value = parameters.GetVector("_VelocityOverLifetimeStart");
            return new Vector3(value.x, value.y, value.z);
        }

        [PublicAPI]
        public static void SetStart(this FluxParticleVelocityOverLifetimeParameters parameters, Vector3 start)
        {
            parameters.SetVector("_VelocityOverLifetimeStart", new Vector4(start.x, start.y, start.z, 0));
        }

        [PublicAPI]
        public static Vector3 GetEnd(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            var value = parameters.GetVector("_VelocityOverLifetimeEnd");
            return new Vector3(value.x, value.y, value.z);
        }

        [PublicAPI]
        public static void SetEnd(this FluxParticleVelocityOverLifetimeParameters parameters, Vector3 end)
        {
            parameters.SetVector("_VelocityOverLifetimeEnd", new Vector4(end.x, end.y, end.z, 0));
        }

        [PublicAPI]
        public static Vector3 GetOrbital(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            var value = parameters.GetVector("_VelocityOverLifetimeOrbital");
            return new Vector3(value.x, value.y, value.z);
        }

        [PublicAPI]
        public static void SetOrbital(this FluxParticleVelocityOverLifetimeParameters parameters, Vector3 orbital)
        {
            parameters.SetVector("_VelocityOverLifetimeOrbital", new Vector4(orbital.x, orbital.y, orbital.z, 0));
        }

        [PublicAPI]
        public static Vector3 GetOffset(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            var value = parameters.GetVector("_VelocityOverLifetimeOffset");
            return new Vector3(value.x, value.y, value.z);
        }

        [PublicAPI]
        public static void SetOffset(this FluxParticleVelocityOverLifetimeParameters parameters, Vector3 offset)
        {
            parameters.SetVector("_VelocityOverLifetimeOffset", new Vector4(offset.x, offset.y, offset.z, 0));
        }

        [PublicAPI]
        public static float GetRadial(this FluxParticleVelocityOverLifetimeParameters parameters)
        {
            return parameters.GetFloat("_VelocityOverLifetimeRadial");
        }

        [PublicAPI]
        public static void SetRadial(this FluxParticleVelocityOverLifetimeParameters parameters, float radial)
        {
            parameters.SetFloat("_VelocityOverLifetimeRadial", radial);
        }
    }
}