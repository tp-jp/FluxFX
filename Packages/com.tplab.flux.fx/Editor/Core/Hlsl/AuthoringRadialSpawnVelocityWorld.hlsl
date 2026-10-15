#define FluxFX_SpawnVelocity FluxFX_SpawnVelocityLocal
#include "Packages/com.tplab.flux.fx/Editor/Core/Hlsl/AuthoringRadialSpawnVelocity.hlsl"
#undef FluxFX_SpawnVelocity

void FluxFX_SpawnVelocity(inout FluxSpawnVelocityContext ctx)
{
    FluxFX_SpawnVelocityLocal(ctx);
    float4 value = float4(ctx.valueParticleVelocity, 0.0);
    ctx.valueParticleVelocity = float3(dot(ctx.valueEmitterRow0, value),
        dot(ctx.valueEmitterRow1, value), dot(ctx.valueEmitterRow2, value));
}