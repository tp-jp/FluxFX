Shader "Hidden/FluxFX/Program/FreeReduction"
{
    SubShader { Pass {
        ZTest Always Cull Off ZWrite Off Blend Off
        HLSLPROGRAM
        #pragma target 3.5
        #pragma vertex vert_img
        #pragma fragment frag
        #include "UnityCG.cginc"
        sampler2D _FluxSource;
        float4 _FluxSourceSize;
        float4 _FluxDestinationSize;
        float4 frag(v2f_img input) : SV_Target
        {
            uint2 pixel = (uint2)floor(input.uv * _FluxDestinationSize.xy);
            uint index = pixel.y * (uint)_FluxDestinationSize.x + pixel.x;
            float sum = 0;
            if (index >= (uint)_FluxDestinationSize.z) return 0;
            [unroll] for (uint child = 0; child < 4; child++)
            {
                uint sourceIndex = index * 4 + child;
                if (sourceIndex < (uint)_FluxSourceSize.z)
                {
                    float2 uv = (float2(sourceIndex % (uint)_FluxSourceSize.x, sourceIndex / (uint)_FluxSourceSize.x) + 0.5) / _FluxSourceSize.xy;
                    sum += tex2Dlod(_FluxSource, float4(uv, 0, 0)).r;
                }
            }
            return float4(sum, 0, 0, 0);
        }
        ENDHLSL
    } }
    Fallback Off
}
