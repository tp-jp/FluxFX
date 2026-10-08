using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleColorOverLifetimeModule : FluxParticleModule
    {
        [SerializeField]
        FluxParticleColorOverLifetimeMode mode;

        [SerializeField]
        Color endColor = Color.white;

        [SerializeField]
        Gradient gradient = new Gradient();

        public FluxParticleColorOverLifetimeMode Mode => mode;

        public Color EndColor => endColor;

        public Gradient Gradient => gradient;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.Rendering;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Age |
            FluxParticleAttribute.Lifetime |
            FluxParticleAttribute.StartColor;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.None;
    }
}