void FluxFX_Motion(inout FluxMotionContext ctx)
{
    ctx.valueParticlePosition += ctx.valueParticleVelocity * ctx.valueStepDeltaTime;
    ctx.valueParticleAge += ctx.valueStepDeltaTime;
}
