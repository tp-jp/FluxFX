Shader "Hidden/FluxFX/Program/AllocatedSpawnKey"
{
    SubShader { Pass {
        ZTest Always Cull Off ZWrite Off Blend Off
        HLSLPROGRAM
        #pragma target 3.5
        #pragma vertex vert_img
        #pragma fragment frag
        #include "UnityCG.cginc"
        sampler2D _FluxAllocation;
        float _FluxSpawnSeedBase;
        float4 frag(v2f_img input) : SV_Target
        {
            float2 allocation = tex2D(_FluxAllocation, input.uv).rg;
            return float4(allocation.r < 0.5 ? 0u : ((uint)_FluxSpawnSeedBase + (uint)allocation.g) % 1048576u, 0, 0, 0);
        }
        ENDHLSL
    } }
    Fallback Off
}
