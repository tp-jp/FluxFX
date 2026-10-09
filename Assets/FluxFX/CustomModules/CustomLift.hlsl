#ifndef FLUXFX_CUSTOM_LIFT_INCLUDED
#define FLUXFX_CUSTOM_LIFT_INCLUDED

void FluxFX_CustomLift(inout float3 velocity, float deltaTime)
{
    float strength = _CustomLiftStrength * (0.5 + 0.5 * sin(_FluxFXSimulationTime * 2.0));
    velocity += _CustomLiftDirection.xyz * strength * deltaTime;
}

#endif