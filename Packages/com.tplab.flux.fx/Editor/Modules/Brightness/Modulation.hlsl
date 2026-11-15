#ifndef FLUXFX_BRIGHTNESS_MODULATION_INCLUDED
#define FLUXFX_BRIGHTNESS_MODULATION_INCLUDED
uint FluxFX_BrightnessHash(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    return value ^ (value >> 16);
}

float FluxFX_BrightnessVariation(uint key)
{
    return (FluxFX_BrightnessHash(key) & 65535u) / 65535.0;
}

float FluxFX_BrightnessPulse(float phase, float duty, float smoothness)
{
    float pulse = duty <= 0 ? 0 : (phase < duty ? 1 : 0);
    float edge = min(duty, 1 - duty) * 0.5 * smoothness;
    if (duty > 0 && edge > 0)
    {
        float distance = abs(frac(phase - duty * 0.5 + 0.5) - 0.5);
        pulse = 1 - smoothstep(duty * 0.5 - edge, duty * 0.5 + edge, distance);
    }
    return pulse;
}

float FluxFX_BrightnessTemporal(uint key, float time, float waveform, float period,
    float periodMinimum, float periodMaximum, float randomPhase, float minimum, float maximum,
    float duty, float smoothness, bool preserveBlinkPhase)
{
    // Zero period denotes a stopped oscillator with identity output, including migrated zero Hz.
    if (waveform < 0.5 || period <= 0) return 1;
    float speed = FluxFX_BrightnessVariation(key + 7919u);
    period *= lerp(min(periodMinimum, periodMaximum), max(periodMinimum, periodMaximum), speed);
    float random = FluxFX_BrightnessVariation(key + 1723u);
    if (waveform < 1.5)
    {
        float phase = random * randomPhase;
        float wave = sin(6.2831853 * (frac(time / max(period, 0.01)) + phase));
        return (minimum + maximum) * 0.5 + (maximum - minimum) * 0.5 * wave;
    }
    // Migrated Blink data retains its phase; new Pulse and Sine share the integer hash.
    if (duty >= 1) return maximum;
    if (preserveBlinkPhase) random = frac((float)key * 0.61803398875);
    float phase = frac(time * (1.0 / period) + random * randomPhase);
    return lerp(minimum, maximum, FluxFX_BrightnessPulse(phase, duty, smoothness));
}

float FluxFX_BrightnessResponse(float state, float target, float deltaTime, float attack, float release)
{
    float seconds = target > state ? attack : release;
    state = lerp(state, target, 1.0 - exp(-max(deltaTime, 0.0) / max(seconds, 0.01)));
    return target == 0.0 && state < 0.00001 ? 0.0 : state;
}
#endif
