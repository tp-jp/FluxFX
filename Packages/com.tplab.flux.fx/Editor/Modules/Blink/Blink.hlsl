void FluxFX_Blink(inout FluxRenderBlinkContext ctx)
{
    float multiplier = 1;
    float frequency = ctx.parameterBlinkFrequency;
    float duty = ctx.parameterBlinkDuty;
    if (frequency > 0 && duty < 1)
    {
        float time = ctx.parameterBlinkTimeMode < 0.5 ? ctx.valueStepTime : ctx.valueParticleAge;
        float random = frac((float)ctx.valueParticleSpawnKey * 0.61803398875);
        float phase = frac(time * frequency + random * ctx.parameterBlinkRandomPhase);
        float pulse = duty <= 0 ? 0 : (phase < duty ? 1 : 0);
        float edge = min(duty, 1 - duty) * 0.5 * ctx.parameterBlinkSmoothness;
        if (duty > 0 && edge > 0)
        {
            float distance = abs(frac(phase - duty * 0.5 + 0.5) - 0.5);
            pulse = 1 - smoothstep(duty * 0.5 - edge, duty * 0.5 + edge, distance);
        }
        multiplier = lerp(ctx.parameterBlinkMinimum, 1, pulse);
    }
    if (ctx.parameterBlinkTarget < 0.5 || ctx.parameterBlinkTarget > 1.5)
    {
        ctx.valueRenderColor.rgb *= multiplier;
    }
    if (ctx.parameterBlinkTarget > 0.5)
    {
        ctx.valueRenderColor.a *= multiplier;
    }
}
