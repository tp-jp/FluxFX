void FluxFX_Noise(inout FluxNoiseContext ctx)
{
    float3 p = ctx.valueParticlePosition * ctx.parameterNoiseScale;
    float time = ctx.valueStepTime * ctx.parameterNoiseSpeed;
    float3 noise;
    noise.x = sin(p.y * 1.37 + p.z * 0.73 + time);
    noise.y = sin(p.z * 1.11 + p.x * 1.53 + time * 1.17);
    noise.z = sin(p.x * 0.91 + p.y * 1.29 + time * 0.83);
    ctx.valueParticleVelocity += noise * ctx.parameterNoiseStrength * ctx.valueStepDeltaTime;
}
