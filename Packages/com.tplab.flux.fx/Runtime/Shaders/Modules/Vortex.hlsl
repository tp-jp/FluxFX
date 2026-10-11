#ifndef FLUXFX_VORTEX_INCLUDED
#define FLUXFX_VORTEX_INCLUDED

void FluxFX_Vortex(inout FluxFXGpuContext ctx)
{
    float3 offset = ctx.position - _VortexCenter.xyz;

    float axialDistance = dot(offset, _VortexAxis.xyz);
    float3 radialOffset = offset - _VortexAxis.xyz * axialDistance;

    float radialLength = length(radialOffset);

    if (radialLength <= 0.0001)
        return;

    float3 radialDirection = radialOffset / radialLength;
    float3 tangent = cross(_VortexAxis.xyz, radialDirection);

    ctx.velocity += tangent * _VortexStrength * ctx.deltaTime;
}

#endif