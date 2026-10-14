#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float3 FluxFX_SpawnDirection(uint spawnKey)
{
    float angle = FluxFXRandom01(spawnKey * 31u + 17u) * 6.28318530718;
    return float3(cos(angle), 0.0, sin(angle));
}