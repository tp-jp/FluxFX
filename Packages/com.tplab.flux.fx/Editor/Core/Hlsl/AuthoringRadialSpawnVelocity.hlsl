void FluxFX_SpawnVelocity(inout FluxSpawnVelocityContext ctx)
{
    ctx.valueParticleVelocity = ctx.valueSpawnDirection * ctx.parameterSpawnSpeed;
    ctx.valueParticleSpawnKey = ctx.valueSpawnKey;
}
