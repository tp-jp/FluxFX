#include "Packages/com.tplab.flux.fx/Editor/Modules/Brightness/Modulation.hlsl"
void FluxFX_BrightnessApply(inout FluxRenderBrightnessContext ctx, bool preserveBlinkPhase)
{
    float variation = FluxFX_BrightnessVariation(ctx.valueParticleSpawnKey);
    float minimum = max(0.0, min(ctx.parameterBrightnessBaseMinimum, ctx.parameterBrightnessBaseMaximum));
    float maximum = max(minimum, max(ctx.parameterBrightnessBaseMinimum, ctx.parameterBrightnessBaseMaximum));
    float baseline = lerp(minimum, maximum, variation);
    float time = ctx.parameterBrightnessTimeMode < 0.5 ? ctx.valueStepTime : ctx.valueParticleAge;
    float temporal = FluxFX_BrightnessTemporal(ctx.valueParticleSpawnKey, time,
        ctx.parameterBrightnessWaveform, ctx.parameterBrightnessPeriod,
        ctx.parameterBrightnessPeriodMinimum, ctx.parameterBrightnessPeriodMaximum,
        ctx.parameterBrightnessRandomPhase, ctx.parameterBrightnessMinimum, ctx.parameterBrightnessMaximum,
        ctx.parameterBrightnessDuty, ctx.parameterBrightnessSmoothness, preserveBlinkPhase);
    float level = saturate(ctx.valueBrightnessAudio);
    float audio = 1.0 + level * level * ctx.parameterBrightnessAudioBoost * lerp(0.65, 1.0, variation);
    float multiplier = baseline * temporal * audio;
    if (ctx.parameterBrightnessTarget < 0.5 || ctx.parameterBrightnessTarget > 1.5)
    {
        ctx.valueRenderColor.rgb *= multiplier;
    }
    if (ctx.parameterBrightnessTarget > 0.5)
    {
        ctx.valueRenderColor.a *= multiplier;
    }
}

void FluxFX_BrightnessRender(inout FluxRenderBrightnessContext ctx)
{
    FluxFX_BrightnessApply(ctx, false);
}

void FluxFX_BrightnessRenderWithBlinkPhase(inout FluxRenderBrightnessContext ctx)
{
    FluxFX_BrightnessApply(ctx, true);
}
