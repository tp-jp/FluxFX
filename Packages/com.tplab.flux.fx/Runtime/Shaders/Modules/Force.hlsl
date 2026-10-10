#ifndef FLUXFX_FORCE_INCLUDED
#define FLUXFX_FORCE_INCLUDED

void FluxFX_Force(inout FluxFXGpuContext ctx)
{
    ctx.velocity += _Force.xyz * ctx.deltaTime;
}

#endif