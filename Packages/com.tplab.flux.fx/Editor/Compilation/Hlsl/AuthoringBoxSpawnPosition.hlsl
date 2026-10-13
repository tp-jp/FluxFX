#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnPosition(inout FluxSpawnPositionContext ctx)
{
    uint key = ctx.valueSpawnKey;
    float3 randomPosition = float3(
        FluxFXRandom01(key * 31u + 17u),
        FluxFXRandom01(key * 37u + 23u),
        FluxFXRandom01(key * 41u + 29u));

    ctx.valueParticlePosition = ctx.parameterSpawnPosition +
        (randomPosition - 0.5) * ctx.parameterShapeSize;
    ctx.valueParticleAge = 0.0;
}