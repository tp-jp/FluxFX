using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleGravityModule : FluxParticleModule
    {
        [SerializeField]
        Vector3 gravity = new Vector3(0, -1.0f, 0);

        public Vector3 Gravity => gravity;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.Velocity;
    }
}