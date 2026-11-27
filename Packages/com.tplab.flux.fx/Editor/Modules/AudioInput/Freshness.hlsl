void FluxFX_AudioInputFreshness(inout FluxAudioInputUpdateFreshnessContext ctx)
{
    float frame = ctx.valueAudioInputSample.z;
    float2 previous = ctx.valueAudioInputFreshness;
    // 未取得は-1、初回観測後の更新確認待ちは-(frame + 2)。既存のx >= 0契約を保つ。
    if (ctx.parameterAudioInputEnabled < 0.5 || !(frame >= 0))
    {
        ctx.valueAudioInputFreshness = float2(-1, 0);
        return;
    }
    if (previous.x == -1)
    {
        ctx.valueAudioInputFreshness = float2(-(frame + 2), 0);
        return;
    }
    float baseline = previous.x < -1 ? -previous.x - 2 : previous.x;
    // 周回を含め、大小ではなく変化で更新を確認する。
    if (frame != baseline)
    {
        ctx.valueAudioInputFreshness = float2(frame, 0);
        return;
    }
    float stale = min(previous.y + max(ctx.valueStepDeltaTime, 0), ctx.parameterAudioInputTimeout);
    ctx.valueAudioInputFreshness = float2(previous.x, stale);
}
