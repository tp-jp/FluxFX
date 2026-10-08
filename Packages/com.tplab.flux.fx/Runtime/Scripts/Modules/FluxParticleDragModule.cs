using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleDragModule : FluxParticleModule
    {
        [SerializeField]
        [Min(0)]
        float drag = 0.5f;

        public float Drag => drag;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.Velocity;
    }
}