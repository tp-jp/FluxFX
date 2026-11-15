#include "Packages/com.tplab.flux.fx/Editor/Modules/Brightness/Modulation.hlsl"
void FluxFX_BrightnessUpdate(inout FluxBrightnessUpdateContext ctx)
{
    bool active = ctx.parameterBrightnessEnabled > 0.5 && ctx.parameterAudioInputEnabled > 0.5
        && ctx.valueAudioInputFreshness.x >= 0.0
        && ctx.valueAudioInputFreshness.y < ctx.parameterAudioInputTimeout;
    float target = active ? saturate(ctx.valueAudioInputSample.y) : 0.0;
    ctx.valueBrightnessAudio = FluxFX_BrightnessResponse(ctx.valueBrightnessAudio, target,
        ctx.valueStepDeltaTime, ctx.parameterBrightnessAudioAttack, ctx.parameterBrightnessAudioRelease);
}
