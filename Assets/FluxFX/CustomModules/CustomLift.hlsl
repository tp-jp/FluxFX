#ifndef FLUXFX_CUSTOM_LIFT_INCLUDED
#define FLUXFX_CUSTOM_LIFT_INCLUDED

void FluxFX_CustomLift(inout float3 velocity, float deltaTime)
{
    velocity.y += 2.0 * deltaTime;
}

#endif
