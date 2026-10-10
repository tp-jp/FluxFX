#ifndef FLUXFX_NOISE_INCLUDED
#define FLUXFX_NOISE_INCLUDED

void FluxFX_Noise(inout FluxFXGpuContext ctx)
{
    float3 p = ctx.position * _NoiseScale;
    float time = ctx.simulationTime * _NoiseSpeed;

    float3 noise;

    noise.x = sin(p.y * 1.37 + p.z * 0.73 + time);
    noise.y = sin(p.z * 1.11 + p.x * 1.53 + time * 1.17);
    noise.z = sin(p.x * 0.91 + p.y * 1.29 + time * 0.83);

    ctx.velocity += noise * _NoiseStrength * ctx.deltaTime;
}

#endif