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
                uint2 pixel = (uint2)floor(i.uv * _FluxStateSize.xy);
                uint capacity = (uint)_FluxStateSize.z;
                uint index = pixel.y * (uint)_FluxStateSize.x + pixel.x;
                uint offset = (index + capacity - (uint)_FluxSpawnFirst) % capacity;
                float active = index < capacity && offset < (uint)_FluxSpawnCount ? 1.0 : 0.0;
                return float4(active, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
