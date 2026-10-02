#ifndef FLUX_FX_PARTICLE_COMMON_INCLUDED
#define FLUX_FX_PARTICLE_COMMON_INCLUDED

float FluxFXRandom01(uint value)
{
    return frac(sin((float)value * 12.9898) * 43758.5453);
}

float3 FluxFXCreateSpawnDirection(uint spawnSequence)
{
    float goldenAngle = 2.39996323;
    float y = FluxFXRandom01(spawnSequence * 5u + 1u) * 2.0 - 1.0;
    float radius = sqrt(max(0.0, 1.0 - y * y));
    float angle = spawnSequence * goldenAngle;

    return float3(
        cos(angle) * radius,
        y,
        sin(angle) * radius
    );
}

uint FluxFXGetSpawnSequence(uint index, uint capacity, uint spawnStart, uint spawnCount)
{
    for (uint i = spawnCount; i > 0; i--)
    {
        uint sequence = spawnStart + i - 1;

        if (sequence % capacity == index)
        {
            return sequence;
        }
    }

    return 0;
}

#endif