void FluxFX_Vortex(inout FluxVortexContext ctx)
{
    float3 axis = ctx.parameterVortexAxis;
    float scale = max(abs(axis.x), max(abs(axis.y), abs(axis.z)));
    if (scale <= 1e-6) return;
    axis /= scale;
    axis *= rsqrt(dot(axis, axis));
    float3 offset = ctx.valueParticlePosition - ctx.parameterVortexCenter;
    float3 radial = offset - axis * dot(offset, axis);
    float distance = length(radial);
    if (distance <= 1e-4) return;
    ctx.valueParticleVelocity += cross(axis, radial / distance) * ctx.parameterVortexStrength * ctx.valueStepDeltaTime;
}
