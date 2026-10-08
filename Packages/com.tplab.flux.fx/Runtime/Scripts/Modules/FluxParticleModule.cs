using System;
using UnityEngine;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Serializable]
    public abstract class FluxParticleModule
    {
        [SerializeField]
        bool enabled = true;

        public bool Enabled => enabled;

        public abstract FluxParticleExecutionStage Stage { get; }

        public abstract FluxParticleAttribute ReadAttributes { get; }

        public abstract FluxParticleAttribute WriteAttributes { get; }
    }
}