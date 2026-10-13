Shader "Hidden/FluxFX/Program/SpawnMask"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _FluxStateSize;
            float _FluxSpawnFirst;
            float _FluxSpawnCount;

            float4 frag(v2f_img i) : SV_Target
            {
                float2 pixel = floor(i.uv * _FluxStateSize.xy);
                float index = pixel.y * _FluxStateSize.x + pixel.x;
                float offset = fmod(index - _FluxSpawnFirst + _FluxStateSize.z, _FluxStateSize.z);
                float active = index < _FluxStateSize.z && offset < _FluxSpawnCount ? 1.0 : 0.0;
                return float4(active, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
