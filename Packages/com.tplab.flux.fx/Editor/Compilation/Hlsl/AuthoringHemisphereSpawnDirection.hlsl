#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float3 FluxFX_SpawnDirection(uint spawnKey)
{
    float3 direction = FluxFXCreateSpawnDirection(spawnKey * 31u + 17u);
    direction.y = abs(direction.y);
    return direction;
}
