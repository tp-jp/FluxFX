using System;

namespace TpLab.Flux.FX.Scripts.Modules
{
    [Flags]
    public enum FluxParticleAttribute
    {
        None = 0,

        Position = 1 << 0,
        Age = 1 << 1,
        Velocity = 1 << 2,
        Lifetime = 1 << 3,
        StartColor = 1 << 4,
        StartSize = 1 << 5,
        StartRotation = 1 << 6
    }

    [Flags]
    public enum FluxParticleExecutionStage
    {
        None = 0,

        Spawn = 1 << 0,
        VelocityUpdate = 1 << 1,
        PositionUpdate = 1 << 2,
        SpawnAttributes = 1 << 3,
        Rendering = 1 << 4
    }
}