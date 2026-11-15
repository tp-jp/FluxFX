// Adapts the frozen reference to the current renderer without registering an old module.
#include "LegacyPulseReference.hlsl"
void FluxFX_BrightnessRenderWithBlinkPhase(inout FluxRenderBrightnessContext ctx)
{
    FluxFX_TestLegacyPulseContext legacy = (FluxFX_TestLegacyPulseContext)0;
    legacy.parameterBlinkFrequency = ctx.parameterBrightnessPeriod > 0 ? 1.0 / ctx.parameterBrightnessPeriod : 0;
    legacy.parameterBlinkDuty = ctx.parameterBrightnessDuty;
    legacy.parameterBlinkMinimum = ctx.parameterBrightnessMinimum;
    legacy.parameterBlinkSmoothness = ctx.parameterBrightnessSmoothness;
    legacy.parameterBlinkRandomPhase = ctx.parameterBrightnessRandomPhase;
    legacy.parameterBlinkTimeMode = ctx.parameterBrightnessTimeMode;
    legacy.parameterBlinkTarget = ctx.parameterBrightnessTarget;
    legacy.valueParticleAge = ctx.valueParticleAge;
    legacy.valueParticleSpawnKey = ctx.valueParticleSpawnKey;
    legacy.valueRenderColor = ctx.valueRenderColor;
    legacy.valueStepTime = ctx.valueStepTime;
    FluxFX_TestLegacyPulse(legacy);
    ctx.valueRenderColor = legacy.valueRenderColor;
}
