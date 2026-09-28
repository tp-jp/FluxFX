Shader "FluxFX/Tests/ParticleUpdate"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
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
            float _DeltaTime;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float4 position = tex2D(_MainTex, FluxGetSourceUV(index));

                position.x += 0.2 * _DeltaTime;

                return position;
            }

            ENDHLSL
        }
    }
}