void FluxFX_Drag(inout FluxDragContext ctx)
{
    ctx.valueParticleVelocity *= exp(-max(ctx.parameterDrag, 0.0) * ctx.valueStepDeltaTime);
}
