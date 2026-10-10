#ifndef FLUXFX_GRAVITY_INCLUDED
#define FLUXFX_GRAVITY_INCLUDED

void FluxFX_Gravity(inout FluxFXGpuContext ctx)
{
    ctx.velocity += _Gravity.xyz * ctx.deltaTime;
}

#endif