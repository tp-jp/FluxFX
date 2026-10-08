using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleVelocityOverLifetimeModule : FluxParticleModule
    {
        [SerializeField]
        Vector3 start;

        [SerializeField]
        Vector3 end;

        [SerializeField]
        Vector3 orbital;

        [SerializeField]
        Vector3 offset;

        [SerializeField]
        float radial;

        public Vector3 Start => start;

        public Vector3 End => end;

        public Vector3 Orbital => orbital;

        public Vector3 Offset => offset;

        public float Radial => radial;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.PositionUpdate;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Position |
            FluxParticleAttribute.Age |
            FluxParticleAttribute.Velocity |
            FluxParticleAttribute.Lifetime;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.Position;
    }
}