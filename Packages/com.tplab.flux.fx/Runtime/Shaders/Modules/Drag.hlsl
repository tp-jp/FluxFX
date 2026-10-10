#ifndef FLUXFX_DRAG_INCLUDED
#define FLUXFX_DRAG_INCLUDED

void FluxFX_Drag(inout FluxFXGpuContext ctx)
{
    ctx.velocity *= exp(-_Drag * ctx.deltaTime);
}

#endif