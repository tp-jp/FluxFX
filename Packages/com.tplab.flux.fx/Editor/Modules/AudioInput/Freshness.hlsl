void FluxFX_AudioInputFreshness(inout FluxAudioInputUpdateFreshnessContext ctx)
{
    float frame = ctx.valueAudioInputSample.z;
    float2 previous = ctx.valueAudioInputFreshness;
    float stale = frame >= 0 && frame != previous.x ? 0
        : min(previous.y + max(ctx.valueStepDeltaTime, 0), ctx.parameterAudioInputTimeout);
    ctx.valueAudioInputFreshness = float2(frame, stale);
}
