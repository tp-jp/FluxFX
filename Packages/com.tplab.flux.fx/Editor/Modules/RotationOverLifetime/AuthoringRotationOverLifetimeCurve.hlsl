void FluxFX_RenderRotationOverLifetime(inout FluxRenderRotationOverLifetimeContext ctx)
{
    ctx.valueRenderRotation += ctx.parameterRotationEnd * ctx.valueRotationCurve;
}
