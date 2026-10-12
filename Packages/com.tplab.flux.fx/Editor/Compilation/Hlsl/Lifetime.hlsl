// SpawnKey is bounded to 20 bits. This deterministic hash uses exactly representable float integers.
float FluxFX_Lifetime(uint spawnKey, float2 range)
{
    float random = frac((float)spawnKey * 0.61803398875);
    return lerp(range.x, range.y, random);
}
