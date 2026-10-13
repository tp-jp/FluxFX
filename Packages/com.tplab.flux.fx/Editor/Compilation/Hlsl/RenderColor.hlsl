void FluxFX_RenderColor(inout FluxRenderColorContext ctx)
{
    float normalizedAge = saturate(ctx.valueParticleAge / max(ctx.valueParticleLifetime, 1e-6));
    ctx.valueRenderColor = lerp(ctx.valueRenderColor, ctx.parameterColorEnd, normalizedAge);
}
