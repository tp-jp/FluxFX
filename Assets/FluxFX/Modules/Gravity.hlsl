#ifndef FLUXFX_GRAVITY_INCLUDED
#define FLUXFX_GRAVITY_INCLUDED

void FluxFX_Gravity(inout float3 velocity, float deltaTime)
{
    velocity += _Gravity.xyz * deltaTime;
}

#endif
