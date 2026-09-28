Shader "Flux/Tests/FluxKernel/VectorScale"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _Scale ("Scale", Float) = 1
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
            float _Scale;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float4 value = tex2D(_MainTex, FluxGetSourceUV(index));

                return value * _Scale;
            }

            ENDHLSL
        }
    }
}