using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleSizeOverLifetimeModule : FluxParticleModule
    {
        [SerializeField]
        FluxParticleSizeOverLifetimeMode mode;

        [SerializeField]
        [Min(0)]
        float endSize;

        [SerializeField]
        AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 1);

        public FluxParticleSizeOverLifetimeMode Mode => mode;

        public float EndSize => endSize;

        public AnimationCurve Curve => curve;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.Rendering;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Age |
            FluxParticleAttribute.Lifetime |
            FluxParticleAttribute.StartSize;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.None;
    }
}