void FluxFX_SizeOverLifetime(inout FluxRenderSizeOverLifetimeContext ctx)
{
    ctx.valueRenderSize = max(0.0, ctx.valueRenderSize * ctx.valueSizeCurve);
}
