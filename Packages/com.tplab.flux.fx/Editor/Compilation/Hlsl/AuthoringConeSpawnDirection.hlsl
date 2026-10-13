#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

float3 FluxFX_SpawnDirection(uint spawnKey, float angleDegrees)
{
    float azimuth = FluxFXRandom01(spawnKey * 31u + 17u) * 6.28318530718;
    float radiusFraction = sqrt(FluxFXRandom01(spawnKey * 37u + 23u));
    float2 disk = float2(cos(azimuth), sin(azimuth)) * radiusFraction;
    float spread = tan(radians(angleDegrees));

    return normalize(float3(disk.x * spread, 1.0, disk.y * spread));
}
