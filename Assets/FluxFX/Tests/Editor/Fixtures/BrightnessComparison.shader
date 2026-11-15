Shader "Hidden/FluxFX/Tests/BrightnessComparison" { SubShader { Pass { ZTest Always ZWrite Off Cull Off
HLSLPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#pragma target 4.5
#include "UnityCG.cginc"
float4 _Settings; // frequency, duty, minimum, smoothness
float4 _Times; // system time, age, time mode, random phase
float4 _Config; // mode (0 pulse,1 sine), target, audio level, output (0 diff,1 old,2 new)
float _PhaseCheck;
float _KeyBase;
struct FluxRenderBrightnessContext {
float parameterBrightnessAudioBoost;
float parameterBrightnessBaseMaximum;
float parameterBrightnessBaseMinimum;
float parameterBrightnessDuty;
float parameterBrightnessMaximum;
float parameterBrightnessMinimum;
float parameterBrightnessPeriod;
float parameterBrightnessPeriodMaximum;
float parameterBrightnessPeriodMinimum;
float parameterBrightnessRandomPhase;
float parameterBrightnessSmoothness;
float parameterBrightnessTarget;
float parameterBrightnessTimeMode;
float parameterBrightnessWaveform;
float valueBrightnessAudio;
float valueParticleAge;
uint valueParticleSpawnKey;
float4 valueRenderColor;
float valueStepTime;
};
#include "LegacyPulseReference.hlsl"

#ifndef ORBITARIUM_MODULATION_INCLUDED
#define ORBITARIUM_MODULATION_INCLUDED
uint ParticleModulation_Hash(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    return value ^ (value >> 16);
}

float ParticleModulation_Response(float current, float target, float deltaTime, float seconds)
{
    return lerp(current, target, 1.0 - exp(-max(deltaTime, 0.0) / max(seconds, 0.01)));
}

float ParticleModulation_Sine(float time, float period, float phase)
{
    return sin(6.2831853 * (frac(time / max(period, 0.01)) + phase));
}
#endif

#ifndef ORBITARIUM_BRIGHTNESS_INCLUDED
#define ORBITARIUM_BRIGHTNESS_INCLUDED


float ParticleBrightness_Evaluate(uint key, float time, float4 settings)
{
    float variation = (ParticleModulation_Hash(key) & 65535u) / 65535.0;
    float phase = (ParticleModulation_Hash(key + 1723u) & 65535u) / 65535.0;
    float speed = (ParticleModulation_Hash(key + 7919u) & 65535u) / 65535.0;
    float minimum = max(0.0, min(settings.x, settings.y));
    float maximum = max(minimum, max(settings.x, settings.y));
    float baseline = lerp(minimum, maximum, variation);
    float period = max(1.0, settings.w) * lerp(0.8, 1.2, speed);
    float shimmer = 1.0 + saturate(settings.z) * ParticleModulation_Sine(time, period, phase);
    return baseline * shimmer;
}
#endif

#include "Packages/com.tplab.flux.fx/Editor/Modules/Brightness/Render.hlsl"
float4 frag(v2f_img input) : SV_Target {
 uint key = (uint)(input.uv.x * 64) + (uint)(input.uv.y * 64) * 64;
 if (_PhaseCheck > 0.5) {
  key += (uint)_KeyBase;
  float pulse = FluxFX_BrightnessTemporal(key, 0, 2, 5, 1, 1, 1, 0, 1, 0.4, 0, false);
  float sine = FluxFX_BrightnessTemporal(key, 0, 1, 5, 1, 1, 1, 0, 1, 0.4, 0, false);
  float oldPulse = FluxFX_BrightnessTemporal(key, 0, 2, 5, 1, 1, 1, 0, 1, 0.4, 0, true);
  return float4(pulse, sine, oldPulse, FluxFX_BrightnessVariation(key + 1723u));
 }
 key = key * 104729u;
 FluxRenderBrightnessContext ctx = (FluxRenderBrightnessContext)0;
 ctx.valueParticleSpawnKey=key;ctx.valueStepTime=_Times.x;ctx.valueParticleAge=_Times.y;
 ctx.parameterBrightnessTimeMode=_Times.z;ctx.parameterBrightnessRandomPhase=_Times.w;
 ctx.parameterBrightnessDuty=_Settings.y;ctx.parameterBrightnessSmoothness=_Settings.w;
 ctx.parameterBrightnessTarget=_Config.y;
 ctx.parameterBrightnessBaseMinimum=1;ctx.parameterBrightnessBaseMaximum=1;
 ctx.parameterBrightnessPeriodMinimum=1;ctx.parameterBrightnessPeriodMaximum=1;
 ctx.parameterBrightnessMinimum=_Settings.z;ctx.parameterBrightnessMaximum=1;
 ctx.parameterBrightnessPeriod=_Settings.x>0 ? 1/_Settings.x : 0;
 ctx.parameterBrightnessWaveform=2;
 float4 original=float4(2,3,4,0.7);
 ctx.valueRenderColor=original;
 float4 reference=original;
 if (_Config.x < 0.5) {
  FluxFX_TestLegacyPulseContext legacy=(FluxFX_TestLegacyPulseContext)0;
  legacy.valueParticleSpawnKey=key;legacy.valueStepTime=_Times.x;legacy.valueParticleAge=_Times.y;
  legacy.parameterBlinkTimeMode=_Times.z;legacy.parameterBlinkRandomPhase=_Times.w;
  legacy.parameterBlinkFrequency=_Settings.x;legacy.parameterBlinkDuty=_Settings.y;
  legacy.parameterBlinkMinimum=_Settings.z;legacy.parameterBlinkSmoothness=_Settings.w;
  legacy.parameterBlinkTarget=_Config.y;legacy.valueRenderColor=original;
  FluxFX_TestLegacyPulse(legacy);reference=legacy.valueRenderColor;
 } else {
  ctx.parameterBrightnessBaseMinimum=6;ctx.parameterBrightnessBaseMaximum=10;
  ctx.parameterBrightnessTimeMode=0;ctx.parameterBrightnessWaveform=1;ctx.parameterBrightnessPeriod=8;
  ctx.parameterBrightnessPeriodMinimum=0.8;ctx.parameterBrightnessPeriodMaximum=1.2;
  ctx.parameterBrightnessRandomPhase=1;ctx.parameterBrightnessMinimum=0.75;ctx.parameterBrightnessMaximum=1.25;
  ctx.valueBrightnessAudio=_Config.z;ctx.parameterBrightnessAudioBoost=2;
  float variation=(ParticleModulation_Hash(key)&65535u)/65535.0;
  float audio=1+_Config.z*_Config.z*2*lerp(0.65,1,variation);
  reference.rgb*=ParticleBrightness_Evaluate(key,_Times.x,float4(6,10,0.25,8))*audio;
 }
 if (_Config.x < 0.5) FluxFX_BrightnessRenderWithBlinkPhase(ctx);
 else FluxFX_BrightnessRender(ctx);
 return _Config.w<0.5 ? abs(reference-ctx.valueRenderColor) : (_Config.w<1.5 ? reference : ctx.valueRenderColor);
}
ENDHLSL
} } }
