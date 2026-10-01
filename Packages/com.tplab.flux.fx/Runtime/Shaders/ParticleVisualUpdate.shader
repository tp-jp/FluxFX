Shader "FluxFX/ParticleVisualUpdate"
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
            #include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

            sampler2D_float _MainTex;
            sampler2D_float _CurrentVelocityTex;
            sampler2D_float _VelocityTex;

            float4 _StartColorMin;
            float4 _StartColorMax;
            float _StartSizeMin;
            float _StartSizeMax;
            float _SpawnStart;
            float _SpawnCount;

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
                    uint spawnSequence = FluxFXGetSpawnSequence(
                        index,
                        (uint)_FluxDestinationCount,
                        (uint)_SpawnStart,
                        (uint)_SpawnCount);
                    float3 startColor = lerp(
                        _StartColorMin.rgb,
                        _StartColorMax.rgb,
                        FluxFXRandom01(spawnSequence * 2u + 1u));
                    float startSize = lerp(
                        _StartSizeMin,
                        _StartSizeMax,
                        FluxFXRandom01(spawnSequence));

                    return float4(startColor, startSize);
                }

                return visual;
            }

            ENDHLSL
        }
    }
}