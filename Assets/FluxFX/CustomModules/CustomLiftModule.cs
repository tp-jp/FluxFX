using System;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules
{
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