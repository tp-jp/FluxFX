using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleVortexModule : FluxParticleModule
    {
        [SerializeField]
        Vector3 center;

        [SerializeField]
        Vector3 axis = Vector3.up;

        [SerializeField]
        [Min(0)]
        float strength = 1.0f;

        public Vector3 Center => center;

        public Vector3 Axis => axis;

        public float Strength => strength;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.VelocityUpdate;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Position | FluxParticleAttribute.Velocity;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.Velocity;
    }
}