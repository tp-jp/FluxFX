#ifndef FLUXFX_VELOCITY_OVER_LIFETIME_INCLUDED
#define FLUXFX_VELOCITY_OVER_LIFETIME_INCLUDED

float3 FluxFX_GetVelocityOverLifetimeCenter()
{
    float3 center = _VelocityOverLifetimeOffset.xyz;

    if (_SimulationSpace > 0.5)
    {
        center *= _SystemScale.xyz;
        center = FluxFX_RotateVector(center, _SystemRotation);
        center += _SystemPosition.xyz;
    }

    return center;
}

float3 FluxFX_GetVelocityOverLifetimeOrbital()
{
    float3 orbital = _VelocityOverLifetimeOrbital.xyz;

    if (_SimulationSpace > 0.5)
    {
        orbital = FluxFX_RotateVector(orbital, _SystemRotation);
    }

    return orbital;
}

void FluxFX_VelocityOverLifetime(inout FluxFXGpuContext ctx)
{
    if (ctx.lifetime <= 0)
        return;

    float previousAge = ctx.age - ctx.deltaTime;
    float3 previousPosition = ctx.position - ctx.velocity * ctx.deltaTime;

    float normalizedAge = saturate(previousAge / ctx.lifetime);

    float3 velocity = lerp(
        _VelocityOverLifetimeStart.xyz,
        _VelocityOverLifetimeEnd.xyz,
        normalizedAge);

    float3 centerOffset = previousPosition - FluxFX_GetVelocityOverLifetimeCenter();
    float3 orbital = FluxFX_GetVelocityOverLifetimeOrbital();

    velocity += cross(orbital, centerOffset);

    float radialDistance = length(centerOffset);

    if (radialDistance > 0.00001)
    {
        velocity += centerOffset / radialDistance * _VelocityOverLifetimeRadial;
    }

    ctx.position += velocity * ctx.deltaTime;
}

#endif