#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float4 FluxFX_StartColor(uint spawnKey, float4 minColor, float4 maxColor)
{
    float random = FluxFXRandom01(spawnKey * 53u + 31u);
    return lerp(minColor, maxColor, random);
}
