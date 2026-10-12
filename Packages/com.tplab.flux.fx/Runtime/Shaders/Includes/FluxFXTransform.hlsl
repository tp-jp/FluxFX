#ifndef FLUXFX_TRANSFORM_INCLUDED
#define FLUXFX_TRANSFORM_INCLUDED

float3 FluxFX_RotateVector(float3 value, float4 rotation)
{
    return value + 2.0 * cross(rotation.xyz, cross(rotation.xyz, value) + rotation.w * value);
}

#endif