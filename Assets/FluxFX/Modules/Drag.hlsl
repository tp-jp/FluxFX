#ifndef FLUXFX_DRAG_INCLUDED
#define FLUXFX_DRAG_INCLUDED

void FluxFX_Drag(inout float3 velocity, float deltaTime)
{
    velocity *= exp(-_Drag * deltaTime);
}

#endif
