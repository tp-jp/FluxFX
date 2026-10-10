#ifndef FLUXFX_CUSTOM_LIFT_INCLUDED
#define FLUXFX_CUSTOM_LIFT_INCLUDED

void FluxFX_CustomLift(inout FluxFXGpuContext ctx)
{
    float strength = _CustomLiftStrength * (0.5 + 0.5 * sin(ctx.simulationTime * 2.0));
    ctx.velocity += _CustomLiftDirection.xyz * strength * ctx.deltaTime;
}

#endif