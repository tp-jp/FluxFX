#include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"
// x: selected band, y: four-band average, z: update counter (-1 when unavailable).
float3 FluxFX_AudioInputSample(float band, float gain, float noiseFloor, float strength, float averageGain)
{
    if (!AudioLinkIsAvailable()) return float3(0, 0, -1);
    float selected = AudioLinkData(ALPASS_AUDIOLINK + uint2(0, (uint)band)).r;
    float average = 0;
    [unroll]
    for (uint i = 0; i < 4; i++)
        average += AudioLinkData(ALPASS_AUDIOLINK + uint2(0, i)).r * 0.25;
    float2 levels = saturate((float2(selected, average) - noiseFloor) / (1 - noiseFloor)
        * gain * float2(1, averageGain)) * strength;
    float frame = fmod(AudioLinkData(ALPASS_GENERALVU + uint2(1, 0)).r, 1024);
    return float3(levels, frame);
}
