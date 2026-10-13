Shader "Hidden/FluxFX/Program/SpawnKey"
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
            float _FluxSpawnSeedBase;

            float4 frag(v2f_img i) : SV_Target
            {
                float2 pixel = floor(i.uv * _FluxStateSize.xy);
                float index = pixel.y * _FluxStateSize.x + pixel.x;
                float offset = fmod(index - _FluxSpawnFirst + _FluxStateSize.z, _FluxStateSize.z);
                float key = index < _FluxStateSize.z && offset < _FluxSpawnCount
                    ? fmod(_FluxSpawnSeedBase + offset, 1048576.0) : 0.0;
                return float4(key, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
