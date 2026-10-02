#ifndef FLUX_FX_PARTICLE_COMMON_INCLUDED
#define FLUX_FX_PARTICLE_COMMON_INCLUDED

#define FLUX_FX_SPAWN_SEED_PERIOD 1048576u
#define FLUX_FX_RANDOM_COMPONENT_PERIOD 4096u

float FluxFXRandom01(uint value)
{
    uint low = value % FLUX_FX_RANDOM_COMPONENT_PERIOD;
    uint high = (value / FLUX_FX_RANDOM_COMPONENT_PERIOD) % FLUX_FX_RANDOM_COMPONENT_PERIOD;

    float lowRandom = frac(sin((float)low * 12.9898) * 43758.5453);
    float highRandom = frac(sin((float)high * 78.233 + 37.719) * 24634.6345);

    return frac(lowRandom + highRandom * 0.61803398875);
}

float FluxFXCreateSpawnAngle(uint spawnSeed)
{
    return FluxFXRandom01(spawnSeed * 7u + 3u) * 6.28318530718;
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
    float y = FluxFXRandom01(spawnSeed * 5u + 1u) * 2.0 - 1.0;
    float radius = sqrt(max(0.0, 1.0 - y * y));
    float angle = FluxFXCreateSpawnAngle(spawnSeed);

    return float3(
        cos(angle) * radius,
        y,
        sin(angle) * radius
    );
}

#endif