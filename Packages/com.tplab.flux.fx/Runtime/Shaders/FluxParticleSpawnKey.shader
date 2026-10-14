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
                uint2 pixel = (uint2)floor(i.uv * _FluxStateSize.xy);
                uint capacity = (uint)_FluxStateSize.z;
                uint index = pixel.y * (uint)_FluxStateSize.x + pixel.x;
                uint offset = (index + capacity - (uint)_FluxSpawnFirst) % capacity;
                float key = index < capacity && offset < (uint)_FluxSpawnCount
                    ? ((uint)_FluxSpawnSeedBase + offset) % 1048576u : 0.0;
                return float4(key, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
