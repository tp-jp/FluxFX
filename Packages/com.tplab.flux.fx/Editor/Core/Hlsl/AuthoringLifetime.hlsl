#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float FluxFX_Lifetime(uint spawnKey, float2 range)
{
    return lerp(range.x, range.y, FluxFXRandom01(spawnKey * 3u + 2u));
}
