void FluxFX_VelocityOverLifetime(inout FluxVelocityOverLifetimeContext ctx)
{
    if (ctx.valueParticleLifetime <= 0.0) return;

    float age = saturate(ctx.valueParticleAge / ctx.valueParticleLifetime);
    float3 velocity = lerp(ctx.parameterVelocityOverLifetimeStart, ctx.parameterVelocityOverLifetimeEnd, age);
    float3 centerOffset = ctx.valueVelocityOverLifetimeCenterOffset;
    velocity += cross(ctx.parameterVelocityOverLifetimeOrbital, centerOffset);
    float distance = length(centerOffset);
    if (distance > 1e-5)
    {
        velocity += centerOffset / distance * ctx.parameterVelocityOverLifetimeRadial;
    }
    ctx.valueParticlePosition += velocity * ctx.valueStepDeltaTime;
}
