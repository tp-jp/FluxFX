void FluxFX_RenderRotationOverLifetime(inout FluxRenderRotationOverLifetimeContext ctx)
{
    float normalizedAge = saturate(ctx.valueParticleAge / max(ctx.valueParticleLifetime, 1e-6));
    ctx.valueRenderRotation += ctx.parameterRotationEnd * normalizedAge;
}
