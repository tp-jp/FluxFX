#ifndef FLUX_FX_PARTICLE_COMMON_INCLUDED
#define FLUX_FX_PARTICLE_COMMON_INCLUDED

#define FLUX_FX_SPAWN_SEED_PERIOD 1048576u

float FluxFXRandom01(uint value)
{
    return frac(sin((float)value * 12.9898) * 43758.5453);
}

uint FluxFXGetSpawnOffset(uint index, uint capacity, uint spawnStart)
{
    uint startIndex = spawnStart % capacity;

    return (index + capacity - startIndex) % capacity;
}

uint FluxFXGetSpawnSeed(uint index, uint capacity, uint spawnStart, uint spawnSeedStart)
{
    uint offset = FluxFXGetSpawnOffset(index, capacity, spawnStart);

    return (spawnSeedStart + offset) % FLUX_FX_SPAWN_SEED_PERIOD;
}

float3 FluxFXCreateSpawnDirection(uint spawnSeed)
{
    float goldenAngle = 2.39996323;
    float y = FluxFXRandom01(spawnSeed * 5u + 1u) * 2.0 - 1.0;
    float radius = sqrt(max(0.0, 1.0 - y * y));
    float angle = spawnSeed * goldenAngle;

    return float3(
        cos(angle) * radius,
        y,
        sin(angle) * radius
    );
}

#endif