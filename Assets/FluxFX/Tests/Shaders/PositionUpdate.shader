Shader "FluxFX/Tests/PositionUpdate"
{
    Properties
    {
        _MainTex ("Position", 2D) = "black" {}
        _VelocityTex ("Velocity", 2D) = "black" {}
        _CurrentVelocityTex ("Current Velocity", 2D) = "black" {}
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
            sampler2D_float _VelocityTex;
            sampler2D_float _CurrentVelocityTex;

            float _DeltaTime;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float2 sourceUV = FluxGetSourceUV(index);

                float4 position = tex2D(_MainTex, sourceUV);
                float4 velocity = tex2D(_VelocityTex, sourceUV);
                float4 currentVelocity = tex2D(_CurrentVelocityTex, sourceUV);

                bool wasActive = currentVelocity.w > 0;
                bool isActive = velocity.w > 0;

                if (!wasActive && isActive)
                    return float4(0, 1.2, 0, 0);

                if (!isActive)
                    return 0;

                position.xyz += velocity.xyz * _DeltaTime;
                position.w += _DeltaTime;

                return position;
            }

            ENDHLSL
        }
    }
}