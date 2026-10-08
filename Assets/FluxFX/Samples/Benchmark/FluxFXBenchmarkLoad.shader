
Shader "Hidden/FluxFX/BenchmarkLoad"
{
    Properties
    {
        _Iterations ("Iterations", Range(1, 256)) = 128
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Iterations;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 6.2831853;
                float value = p.x + p.y;

                for (int j = 0; j < (int)_Iterations; j++)
                {
                    value = sin(value * 1.13 + p.x) * cos(value * 0.97 + p.y);
                    p += float2(value * 0.001, value * 0.002);
                }

                float color = value * 0.5 + 0.5;
                return float4(color, color, color, 1);
            }
            ENDCG
        }
    }
}
