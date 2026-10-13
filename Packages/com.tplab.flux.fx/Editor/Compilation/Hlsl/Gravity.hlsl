void FluxFX_Gravity(inout FluxGravityContext ctx)
{
    ctx.valueParticleVelocity += ctx.parameterGravity * ctx.valueStepDeltaTime;
}
