#ifndef FLUX_FX_PARTICLE_COMMON_INCLUDED
#define FLUX_FX_PARTICLE_COMMON_INCLUDED

float3 FluxFXCreateSpawnDirection(uint index, uint count)
{
    float goldenAngle = 2.39996323;
    float y = 1.0 - 2.0 * ((index + 0.5) / count);
    float radius = sqrt(max(0.0, 1.0 - y * y));
    float angle = index * goldenAngle;

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

float FluxFXRandom01(uint value)
{
    return frac(sin((float)value * 12.9898) * 43758.5453);
}

#endif