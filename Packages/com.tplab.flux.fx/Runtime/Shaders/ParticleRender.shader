Shader "FluxFX/ParticleRender"
{
    Properties
    {
        _PositionTex ("Position", 2D) = "black" {}
        _VelocityTex ("Velocity", 2D) = "black" {}
        _VisualTex ("Visual", 2D) = "white" {}
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
            sampler2D_float _VelocityTex;
            sampler2D_float _VisualTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 particleData : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;

                uint particleIndex = (uint)v.particleData.x;
                float2 stateUV = FluxGetSourceUV(particleIndex);

                float4 position = tex2Dlod(_PositionTex, float4(stateUV, 0, 0));
                float4 velocity = tex2Dlod(_VelocityTex, float4(stateUV, 0, 0));
                float4 visual = tex2Dlod(_VisualTex, float4(stateUV, 0, 0));

                float age = position.w;
                float lifetime = velocity.w;

                if (lifetime <= 0 || age >= lifetime)
                {
                    o.vertex = float4(2, 2, 2, 1);
                    o.color = 0;
                    return o;
                }

                float size = visual.a;
                float3 worldPosition = position.xyz + v.vertex.xyz * size;

                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1));
                o.color = float4(visual.rgb, 1);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }

            ENDHLSL
        }
    }
}