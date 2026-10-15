#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnVelocity(inout FluxSpawnVelocityContext ctx)
{
    ctx.valueParticleVelocity = FluxFXCreateSpawnDirection(ctx.valueSpawnKey) * ctx.parameterSpawnSpeed;
    ctx.valueParticleSpawnKey = ctx.valueSpawnKey;
}
