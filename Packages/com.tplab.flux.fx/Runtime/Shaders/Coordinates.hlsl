#ifndef FLUXFX_COORDINATES_INCLUDED
#define FLUXFX_COORDINATES_INCLUDED
// Persistent particle.position / velocity and render.size are reference-space data.
// World conversion helpers below are for Local simulation; World state is already in world coordinates.
// Apply scale once at the display boundary; intrinsic captures keep reference data.
float3 FluxFX_ReferenceToDisplay(float3 position, float effectScale) { return position * effectScale; }
float3 FluxFX_DisplayToReference(float3 position, float effectScale) { return position / effectScale; }
float3 FluxFX_ReferenceToWorld(float3 position, float4x4 localToWorld, float effectScale)
{
    return mul(localToWorld, float4(FluxFX_ReferenceToDisplay(position, effectScale), 1)).xyz;
}
float3 FluxFX_WorldToReference(float3 position, float4x4 worldToLocal, float effectScale)
{
    return FluxFX_DisplayToReference(mul(worldToLocal, float4(position, 1)).xyz, effectScale);
}
float FluxFX_ReferenceLengthToDisplay(float length, float effectScale) { return length * effectScale; }
// mode 0 follows the effect and existing uniform Transform Scale; mode 1 is fixed world distance.
float FluxFX_DistanceScale(float effectScale, float transformScale, float mode)
{
    return mode < 0.5 ? effectScale * abs(transformScale) : 1.0;
}
#endif
