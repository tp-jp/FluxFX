Shader "FluxFX/Tests/VisualUpdate"
{
    Properties
    {
        _MainTex ("Visual", 2D) = "black" {}
        _CurrentVelocityTex ("Current Velocity", 2D) = "black" {}
        _VelocityTex ("Velocity", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"

            sampler2D_float _MainTex;
            sampler2D_float _CurrentVelocityTex;
            sampler2D_float _VelocityTex;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float2 sourceUV = FluxGetSourceUV(index);

                float4 visual = tex2D(_MainTex, sourceUV);
                float4 currentVelocity = tex2D(_CurrentVelocityTex, sourceUV);
                float4 velocity = tex2D(_VelocityTex, sourceUV);

                bool wasActive = currentVelocity.w > 0;
                bool isActive = velocity.w > 0;

                if (!wasActive && isActive)
                {
                    float t = frac(index * 0.61803398875);

                    float3 startColor = float3(
                        1.0 - t,
                        t,
                        0.25 + t * 0.5
                    );

                    float startSize = 0.5 + t;

                    return float4(startColor, startSize);
                }

                return visual;
            }

            ENDHLSL
        }
    }
}