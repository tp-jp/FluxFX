// The average is formed BEFORE input shaping. Preserve the original accumulation order.
float3 FluxFX_AudioInputSample(float4 bands, float frame, float band, float gain, float noiseFloor, float strength, float averageGain)
{
    if (frame < 0) return float3(0, 0, -1);
    float average = 0;
    [unroll]
    for (uint i = 0; i < 4; i++) average += bands[i] * 0.25;
    float2 levels = saturate((float2(bands[(uint)band], average) - noiseFloor) / (1 - noiseFloor)
        * gain * float2(1, averageGain)) * strength;
    return float3(levels, frame);
}
float4 FluxFX_AudioInputBands(float4 bands, float frame, float gain, float noiseFloor, float strength)
{
    return frame < 0 ? 0 : saturate((bands - noiseFloor) / (1 - noiseFloor) * gain) * strength;
}
