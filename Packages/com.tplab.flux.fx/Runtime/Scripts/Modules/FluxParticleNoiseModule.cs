using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleNoiseModule : FluxParticleModule
    {
        [SerializeField]
        [Min(0)]
        float strength = 1.0f;

        [SerializeField]
        [Min(0)]
        float scale = 1.0f;

        [SerializeField]
        [Min(0)]
        float speed = 1.0f;

        public float Strength => strength;

        public float Scale => scale;

        public float Speed => speed;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Position | FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.Velocity;
    }
}