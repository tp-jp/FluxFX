using System;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Tests.Editor
{
    [Serializable]
    public sealed class UnregisteredParticleModule : FluxParticleModule
    {
        public override FluxParticleExecutionStage Stage => FluxParticleExecutionStage.VelocityUpdate;
        public override FluxParticleAttribute ReadAttributes => FluxParticleAttribute.Velocity;
        public override FluxParticleAttribute WriteAttributes => FluxParticleAttribute.Velocity;
    }
}