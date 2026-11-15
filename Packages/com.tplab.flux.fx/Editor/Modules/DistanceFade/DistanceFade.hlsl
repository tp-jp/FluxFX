#include "Packages/com.tplab.flux.fx/Runtime/Shaders/DistanceFade.hlsl"
void FluxFX_DistanceFadeRender(inout FluxRenderDistanceFadeContext ctx)
{
    // Define this before including the generated render program for density / intrinsic captures.
    #if !defined(FLUXFX_INTRINSIC_RENDER)
        float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
        float scale = length(unity_ObjectToWorld._m00_m10_m20);
        float fade = FluxFX_DistanceFadeForSystem(origin, scale,
            ctx.parameterDistanceFadeStartDistance, ctx.parameterDistanceFadeEndDistance);
        if (ctx.parameterDistanceFadeTarget < 0.5) ctx.valueRenderColor.rgb *= fade;
        else ctx.valueRenderColor.a *= fade;
    #endif
}
