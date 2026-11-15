float FluxFX_AudioSmooth(float current, float target, float dt, float seconds)
{
    float value = lerp(current, target, 1 - exp(-max(dt, 0) / max(seconds, 0.01)));
    return target == 0 && value < 0.00001 ? 0 : value;
}
void FluxFX_AudioInputResponse(inout FluxAudioInputUpdateResponseContext ctx)
{
    float3 state = ctx.valueAudioInputResponse;
    bool active = ctx.parameterAudioInputEnabled > 0.5 && ctx.valueAudioInputFreshness.x >= 0
        && ctx.valueAudioInputFreshness.y < ctx.parameterAudioInputTimeout;
    float2 target = active ? saturate(ctx.valueAudioInputSample.xy) : 0;
    float dt = ctx.valueStepDeltaTime;
    state.x = FluxFX_AudioSmooth(state.x, target.x, dt,
        target.x > state.x ? ctx.parameterAudioInputAttack : ctx.parameterAudioInputRelease);
    state.y = FluxFX_AudioSmooth(state.y, state.x, dt, ctx.parameterAudioInputSlowResponse);
    state.z = FluxFX_AudioSmooth(state.z, target.y, dt,
        target.y > state.z ? ctx.parameterAudioInputAttack : ctx.parameterAudioInputRelease);
    ctx.valueAudioInputResponse = state;
}
