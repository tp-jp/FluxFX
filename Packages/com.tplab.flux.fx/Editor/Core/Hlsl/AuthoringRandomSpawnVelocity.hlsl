#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnVelocity(inout FluxSpawnVelocityContext ctx)
{
    float random = FluxFXRandom01(ctx.valueSpawnKey * 41u + 19u);
    float speed = lerp(ctx.parameterSpawnSpeedRange.x, ctx.parameterSpawnSpeedRange.y, random);
    ctx.valueParticleVelocity = FluxFXCreateSpawnDirection(ctx.valueSpawnKey) * speed;
    ctx.valueParticleSpawnKey = ctx.valueSpawnKey;
}
