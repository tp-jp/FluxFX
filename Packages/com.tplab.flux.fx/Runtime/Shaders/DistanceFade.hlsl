#ifndef FLUXFX_DISTANCE_FADE_INCLUDED
#define FLUXFX_DISTANCE_FADE_INCLUDED
// Distances use source-system local units. systemScale is the magnitude of its uniform world scale.
// A volume with its own transform must pass the source system's origin and scale explicitly.
float FluxFX_DistanceFade(float3 cameraWorld, float3 originWorld, float systemScale,
    float startDistance, float endDistance, bool orthographic)
{
    if (orthographic || endDistance <= startDistance) return 1;
    float distance = length(cameraWorld - originWorld) / max(abs(systemScale), 1e-6);
    return 1 - smoothstep(max(0, startDistance), endDistance, distance);
}
float3 FluxFX_DistanceFadeCameraPosition()
{
    #if defined(USING_STEREO_MATRICES)
        return (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]) * 0.5;
    #else
        return _WorldSpaceCameraPos;
    #endif
}
float FluxFX_DistanceFadeForSystem(float3 originWorld, float systemScale, float startDistance, float endDistance)
{
    return FluxFX_DistanceFade(FluxFX_DistanceFadeCameraPosition(), originWorld, systemScale,
        startDistance, endDistance, unity_OrthoParams.w > 0.5);
}
#endif
