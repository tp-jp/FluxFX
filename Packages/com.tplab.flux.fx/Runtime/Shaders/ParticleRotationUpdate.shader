Shader "FluxFX/ParticleRotationUpdate"
{
    Properties
    {
        _MainTex ("Rotation", 2D) = "black" {}
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

            float3 _StartRotation;
            float _StartAlphaMin;
            float _StartAlphaMax;
            float _SpawnStart;
            float _SpawnSeedStart;
            float _SpawnCount;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float2 sourceUV = FluxGetSourceUV(index);

                float4 rotation = tex2D(_MainTex, sourceUV);
                float4 currentVelocity = tex2D(_CurrentVelocityTex, sourceUV);
                float4 velocity = tex2D(_VelocityTex, sourceUV);

                bool wasActive = currentVelocity.w > 0;
                bool isActive = velocity.w > 0;

                if (!wasActive && isActive)
                {
                    uint spawnSeed = FluxFXGetSpawnSeed(
                        index,
                        (uint)_FluxDestinationCount,
                        (uint)_SpawnStart,
                        (uint)_SpawnSeedStart);

                    float colorRandom = FluxFXRandom01(spawnSeed * 2u + 1u);
                    float startAlpha = lerp(
                        _StartAlphaMin,
                        _StartAlphaMax,
                        colorRandom);

                    return float4(_StartRotation, startAlpha);
                }

                return rotation;
            }

            ENDHLSL
        }
    }
}