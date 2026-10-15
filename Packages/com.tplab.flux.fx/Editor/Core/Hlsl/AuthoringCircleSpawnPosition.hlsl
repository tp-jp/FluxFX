#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnPosition(inout FluxSpawnPositionContext ctx)
{
    uint key = ctx.valueSpawnKey;
    float angle = FluxFXRandom01(key * 31u + 17u) * 6.28318530718;
    float distance = sqrt(FluxFXRandom01(key * 37u + 23u)) * ctx.parameterShapeRadius;

    ctx.valueParticlePosition = ctx.parameterSpawnPosition +
        float3(cos(angle) * distance, 0.0, sin(angle) * distance);
    ctx.valueParticleAge = 0.0;
}