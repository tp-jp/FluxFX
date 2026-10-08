using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleTextureSheetAnimationModule : FluxParticleModule
    {
        [SerializeField]
        [Min(1)]
        int tilesX = 1;

        [SerializeField]
        [Min(1)]
        int tilesY = 1;

        [SerializeField]
        [Min(0)]
        float cycles = 1.0f;

        public int TilesX => tilesX;

        public int TilesY => tilesY;

        public float Cycles => cycles;

        public override FluxParticleExecutionStage Stage =>
            FluxParticleExecutionStage.Rendering;

        public override FluxParticleAttribute ReadAttributes =>
            FluxParticleAttribute.Age |
            FluxParticleAttribute.Lifetime;

        public override FluxParticleAttribute WriteAttributes =>
            FluxParticleAttribute.None;
    }
}