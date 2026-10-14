#define FluxFX_SpawnPosition FluxFX_SpawnPositionLocal
#include "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringBoxSpawnPosition.hlsl"
#undef FluxFX_SpawnPosition

void FluxFX_SpawnPosition(inout FluxSpawnPositionContext ctx)
{
    FluxFX_SpawnPositionLocal(ctx);
    float4 value = float4(ctx.valueParticlePosition, 1.0);
    ctx.valueParticlePosition = float3(dot(ctx.valueEmitterRow0, value),
        dot(ctx.valueEmitterRow1, value), dot(ctx.valueEmitterRow2, value));
}