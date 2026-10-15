void FluxFX_LimitVelocity(inout FluxLimitVelocityContext ctx)
{
    float speed = length(ctx.valueParticleVelocity);
    float maximum = max(0.0, ctx.parameterLimitVelocityMaxSpeed);
    if (speed > maximum)
    {
        ctx.valueParticleVelocity *= maximum / speed;
    }
}
