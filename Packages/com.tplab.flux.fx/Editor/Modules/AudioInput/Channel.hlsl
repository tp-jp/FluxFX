float FluxFX_AudioChannelSample(float4 bands, float3 sample, float source, float gain)
{
    float level = source < 0.5 ? sample.y : bands[(uint)source - 1];
    return saturate(level * gain);
}
void FluxFX_AudioChannelInitialize(out float response)
{
    response = 0;
}
void FluxFX_AudioChannelUpdate(inout float response, float2 freshness, float target, float dt,
    float enabled, float timeout, float attack, float release)
{
    if (enabled < 0.5 || freshness.x < 0 || freshness.y >= timeout) target = 0;
    float seconds = target > response ? attack : release;
    response = lerp(response, target, 1 - exp(-max(dt, 0) / max(seconds, 0.01)));
    if (target == 0 && response < 0.00001) response = 0;
}
