void AudioProbe_Initialize(inout FluxAudioProbeInitializeContext ctx)
{
    ctx.valueAudioProbeResponse = 0;
}
void AudioProbe_Update(inout FluxAudioProbeUpdateContext ctx)
{
    ctx.valueAudioProbeResponse = ctx.valueAudioInputResponse;
}
