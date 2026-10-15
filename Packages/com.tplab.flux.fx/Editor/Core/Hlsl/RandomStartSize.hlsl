#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float FluxFX_StartSize(uint spawnKey, float2 range)
{
    return lerp(range.x, range.y, FluxFXRandom01(spawnKey * 59u + 43u));
}
