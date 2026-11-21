#include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"
float4 FluxFX_AudioInputRawBands()
{
    if (!AudioLinkIsAvailable()) return 0;
    float4 bands;
    [unroll]
    for (uint i = 0; i < 4; i++)
        bands[i] = AudioLinkData(ALPASS_AUDIOLINK + uint2(0, i)).r;
    return bands;
}
float FluxFX_AudioInputFrame()
{
    if (!AudioLinkIsAvailable()) return -1;
    return fmod(AudioLinkData(ALPASS_GENERALVU + uint2(1, 0)).r, 1024);
}
