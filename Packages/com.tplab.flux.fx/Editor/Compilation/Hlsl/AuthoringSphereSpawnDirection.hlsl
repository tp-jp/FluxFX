#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float3 FluxFX_SpawnDirection(uint spawnKey)
{
    return FluxFXCreateSpawnDirection(spawnKey * 31u + 17u);
}
