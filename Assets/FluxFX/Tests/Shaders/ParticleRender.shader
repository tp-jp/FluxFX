Shader "FluxFX/Tests/ParticleRender"
{
    Properties
    {
        _PositionTex ("Position", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"

            sampler2D_float _PositionTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 particleData : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                uint particleIndex = (uint)v.particleData.x;
                float2 positionUV = FluxGetSourceUV(particleIndex);
                float3 position = tex2Dlod(_PositionTex, float4(positionUV, 0, 0)).xyz;

                float3 worldPosition = position + v.vertex.xyz;

                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1));

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(1, 1, 1, 1);
            }

            ENDHLSL
        }
    }
}