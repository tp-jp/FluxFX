#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnPosition(inout FluxSpawnPositionContext ctx)
{
    uint key = ctx.valueSpawnKey;
    float azimuth = FluxFXRandom01(key * 31u + 17u) * 6.28318530718;
    float radiusFraction = sqrt(FluxFXRandom01(key * 37u + 23u));
    float2 disk = float2(cos(azimuth), sin(azimuth)) * radiusFraction;

    ctx.valueParticlePosition = ctx.parameterSpawnPosition +
        float3(disk.x * ctx.parameterShapeRadius, 0.0, disk.y * ctx.parameterShapeRadius);
    ctx.valueParticleAge = 0.0;
}
