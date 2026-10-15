using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    /// <summary>
    /// 最大4096 Slotの軌跡を16点で記録する試作用Moduleです。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleTrailsModule : FluxParticleModule
    {
        [SerializeField]
        [Min(0)]
        float width = 0.05f;

        [SerializeField]
        [ColorUsage(true, true)]
        Color color = Color.white;

        public float Width => width;

        public Color Color => color;

        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.Rendering;

        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Position | FluxParticleAttribute.Age;

        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
    }
}
