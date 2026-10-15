using System;
using UnityEngine;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public sealed class FluxParticleBlinkModule : FluxParticleModule
    {
        [SerializeField]
        [Min(0)]
        float frequency = 2;

        [SerializeField]
        [Range(0, 1)]
        float duty = 0.5f;

        [SerializeField]
        [Range(0, 1)]
        float minimum = 0;

        [SerializeField]
        [Range(0, 1)]
        float smoothness = 0;

        [SerializeField]
        [Range(0, 1)]
        float randomPhase = 0;

        [SerializeField]
        FluxParticleBlinkTimeMode timeMode = FluxParticleBlinkTimeMode.SystemTime;

        [SerializeField]
        FluxParticleBlinkTarget target = FluxParticleBlinkTarget.Alpha;

        public float Frequency => frequency;

        public float Duty => duty;

        public float Minimum => minimum;

        public float Smoothness => smoothness;

        public float RandomPhase => randomPhase;

        public FluxParticleBlinkTimeMode TimeMode => timeMode;

        public FluxParticleBlinkTarget Target => target;

        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.Rendering;

        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Age;

        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.None;
    }
}
