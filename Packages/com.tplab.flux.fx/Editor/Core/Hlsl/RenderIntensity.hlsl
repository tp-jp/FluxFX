void FluxFX_RenderIntensity(inout FluxRenderIntensityContext ctx)
{
    ctx.valueRenderColor.rgb *= ctx.parameterRenderIntensity;
}
