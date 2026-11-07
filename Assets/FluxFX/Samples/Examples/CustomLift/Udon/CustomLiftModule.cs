using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace TpLab.Flux.FX.Samples.CustomLift.Udon
{
    [MovedFrom(true, sourceNamespace: "TpLab.Flux.FX.CustomModules", sourceAssembly: "TpLab.Flux.FX.Samples.CustomLift", sourceClassName: "CustomLiftModule")]
    [Serializable]
    public sealed class CustomLiftModule : FluxParticleModule
    {
        [SerializeField]
        float strength = 2.0f;

        [SerializeField]
        Vector3 direction = Vector3.up;

        public float Strength => strength;

        public Vector3 Direction => direction;
        
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.Velocity;
    }
}