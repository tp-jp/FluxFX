Shader "Hidden/FluxFX/Program/FreeRanks"
{
    SubShader { Pass {
        ZTest Always Cull Off ZWrite Off Blend Off
        HLSLPROGRAM
        #pragma target 3.5
        #pragma vertex vert_img
        #pragma fragment frag
        #include "UnityCG.cginc"
        float4 _FluxStateSize;
        float _FluxSpawnCount;
        float _FluxSpawnFirst;
        float _FluxLevelCount;
        sampler2D _FluxTotal;
        sampler2D _FluxLevel0;
        float4 _FluxLevelSize0;
        float Read0(uint index) { if (index >= (uint)_FluxLevelSize0.z) return 0; return tex2Dlod(_FluxLevel0, float4((float2(index % (uint)_FluxLevelSize0.x, index / (uint)_FluxLevelSize0.x) + 0.5) / _FluxLevelSize0.xy, 0, 0)).r; }

        sampler2D _FluxLevel1;
        float4 _FluxLevelSize1;
        float Read1(uint index) { if (index >= (uint)_FluxLevelSize1.z) return 0; return tex2Dlod(_FluxLevel1, float4((float2(index % (uint)_FluxLevelSize1.x, index / (uint)_FluxLevelSize1.x) + 0.5) / _FluxLevelSize1.xy, 0, 0)).r; }

        sampler2D _FluxLevel2;
        float4 _FluxLevelSize2;
        float Read2(uint index) { if (index >= (uint)_FluxLevelSize2.z) return 0; return tex2Dlod(_FluxLevel2, float4((float2(index % (uint)_FluxLevelSize2.x, index / (uint)_FluxLevelSize2.x) + 0.5) / _FluxLevelSize2.xy, 0, 0)).r; }

        sampler2D _FluxLevel3;
        float4 _FluxLevelSize3;
        float Read3(uint index) { if (index >= (uint)_FluxLevelSize3.z) return 0; return tex2Dlod(_FluxLevel3, float4((float2(index % (uint)_FluxLevelSize3.x, index / (uint)_FluxLevelSize3.x) + 0.5) / _FluxLevelSize3.xy, 0, 0)).r; }

        sampler2D _FluxLevel4;
        float4 _FluxLevelSize4;
        float Read4(uint index) { if (index >= (uint)_FluxLevelSize4.z) return 0; return tex2Dlod(_FluxLevel4, float4((float2(index % (uint)_FluxLevelSize4.x, index / (uint)_FluxLevelSize4.x) + 0.5) / _FluxLevelSize4.xy, 0, 0)).r; }

        sampler2D _FluxLevel5;
        float4 _FluxLevelSize5;
        float Read5(uint index) { if (index >= (uint)_FluxLevelSize5.z) return 0; return tex2Dlod(_FluxLevel5, float4((float2(index % (uint)_FluxLevelSize5.x, index / (uint)_FluxLevelSize5.x) + 0.5) / _FluxLevelSize5.xy, 0, 0)).r; }

        sampler2D _FluxLevel6;
        float4 _FluxLevelSize6;
        float Read6(uint index) { if (index >= (uint)_FluxLevelSize6.z) return 0; return tex2Dlod(_FluxLevel6, float4((float2(index % (uint)_FluxLevelSize6.x, index / (uint)_FluxLevelSize6.x) + 0.5) / _FluxLevelSize6.xy, 0, 0)).r; }

        sampler2D _FluxLevel7;
        float4 _FluxLevelSize7;
        float Read7(uint index) { if (index >= (uint)_FluxLevelSize7.z) return 0; return tex2Dlod(_FluxLevel7, float4((float2(index % (uint)_FluxLevelSize7.x, index / (uint)_FluxLevelSize7.x) + 0.5) / _FluxLevelSize7.xy, 0, 0)).r; }

        sampler2D _FluxLevel8;
        float4 _FluxLevelSize8;
        float Read8(uint index) { if (index >= (uint)_FluxLevelSize8.z) return 0; return tex2Dlod(_FluxLevel8, float4((float2(index % (uint)_FluxLevelSize8.x, index / (uint)_FluxLevelSize8.x) + 0.5) / _FluxLevelSize8.xy, 0, 0)).r; }

        sampler2D _FluxLevel9;
        float4 _FluxLevelSize9;
        float Read9(uint index) { if (index >= (uint)_FluxLevelSize9.z) return 0; return tex2Dlod(_FluxLevel9, float4((float2(index % (uint)_FluxLevelSize9.x, index / (uint)_FluxLevelSize9.x) + 0.5) / _FluxLevelSize9.xy, 0, 0)).r; }

        sampler2D _FluxLevel10;
        float4 _FluxLevelSize10;
        float Read10(uint index) { if (index >= (uint)_FluxLevelSize10.z) return 0; return tex2Dlod(_FluxLevel10, float4((float2(index % (uint)_FluxLevelSize10.x, index / (uint)_FluxLevelSize10.x) + 0.5) / _FluxLevelSize10.xy, 0, 0)).r; }

        sampler2D _FluxLevel11;
        float4 _FluxLevelSize11;
        float Read11(uint index) { if (index >= (uint)_FluxLevelSize11.z) return 0; return tex2Dlod(_FluxLevel11, float4((float2(index % (uint)_FluxLevelSize11.x, index / (uint)_FluxLevelSize11.x) + 0.5) / _FluxLevelSize11.xy, 0, 0)).r; }

        float4 frag(v2f_img input) : SV_Target
        {
            if (_FluxSpawnCount < 1) return 0;
            uint2 pixel = (uint2)floor(input.uv * _FluxStateSize.xy);
            uint index = pixel.y * (uint)_FluxStateSize.x + pixel.x;
            if (index >= (uint)_FluxStateSize.z || Read0(index) < 0.5) return 0;
            uint total = (uint)tex2Dlod(_FluxTotal, float4(0.5, 0.5, 0, 0)).r;
            if (total == 0) return 0;
            uint rank = 0;
            uint childIndex = index;
            if (_FluxLevelCount > 1)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read0(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 2)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read1(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 3)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read2(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 4)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read3(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 5)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read4(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 6)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read5(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 7)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read6(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 8)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read7(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 9)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read8(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 10)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read9(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 11)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read10(first + child);
                }
                childIndex /= 4u;
            }

            if (_FluxLevelCount > 12)
            {
                uint first = (childIndex / 4u) * 4u;
                [unroll] for (uint child = 0; child < 3; child++)
                {
                    if (child < childIndex % 4u) rank += (uint)Read11(first + child);
                }
                childIndex /= 4u;
            }
            uint offset = (rank + total - (uint)_FluxSpawnFirst % total) % total;
            return offset < (uint)_FluxSpawnCount ? float4(1, offset, 0, 0) : 0;
        }
        ENDHLSL
    } }
    Fallback Off
}
