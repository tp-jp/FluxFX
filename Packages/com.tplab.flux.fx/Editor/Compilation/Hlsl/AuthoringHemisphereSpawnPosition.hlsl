#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnPosition(inout FluxSpawnPositionContext ctx)
{
    uint key = ctx.valueSpawnKey;
    float3 direction = FluxFXCreateSpawnDirection(key * 31u + 17u);
    direction.y = abs(direction.y);
    float distance = pow(FluxFXRandom01(key * 37u + 23u), 1.0 / 3.0) * ctx.parameterShapeRadius;

    ctx.valueParticlePosition = ctx.parameterSpawnPosition + direction * distance;
    ctx.valueParticleAge = 0.0;
}
