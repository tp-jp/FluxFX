void FluxFX_SizeOverLifetime(inout FluxRenderSizeOverLifetimeContext ctx)
{
    float age = saturate(ctx.valueParticleAge / max(ctx.valueParticleLifetime, 1e-6));
    ctx.valueRenderSize = lerp(ctx.valueRenderSize, ctx.parameterSizeEnd, age);
}
