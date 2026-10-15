#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float3 FluxFX_StartRotation(uint spawnKey, float3 minRotation, float3 maxRotation)
{
    float3 random = float3(FluxFXRandom01(spawnKey * 67u + 47u),
        FluxFXRandom01(spawnKey * 71u + 53u), FluxFXRandom01(spawnKey * 73u + 59u));
    return lerp(minRotation, maxRotation, random);
}