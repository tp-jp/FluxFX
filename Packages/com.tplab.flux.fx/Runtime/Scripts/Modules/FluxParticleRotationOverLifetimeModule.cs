using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleRotationOverLifetimeModule : FluxParticleModule
    {
        [SerializeField]
        FluxParticleRotationOverLifetimeMode mode;

        [SerializeField]
        Vector3 endRotation;

        [SerializeField]
        AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        public FluxParticleRotationOverLifetimeMode Mode => mode;

        public Vector3 EndRotation => endRotation;

        public AnimationCurve Curve => curve;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.Rendering;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Age |
            FluxParticleAttribute.Lifetime |
            FluxParticleAttribute.StartRotation;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.None;
    }
}