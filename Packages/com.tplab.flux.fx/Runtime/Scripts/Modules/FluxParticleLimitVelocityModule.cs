using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleLimitVelocityModule : FluxParticleModule
    {
        [SerializeField]
        [Min(0)]
        float maxSpeed = 1.0f;

        public float MaxSpeed => maxSpeed;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.Velocity;
    }
}