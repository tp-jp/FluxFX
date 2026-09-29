Shader "FluxFX/Tests/VelocityUpdate"
{
    Properties
    {
        _MainTex ("Velocity", 2D) = "black" {}
        _PositionTex ("Position", 2D) = "black" {}
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
            sampler2D_float _PositionTex;

            float4 _Gravity;
            float _DeltaTime;
            float _SpawnStart;
            float _SpawnCount;

            bool ShouldSpawn(uint index)
            {
                uint capacity = (uint)_FluxDestinationCount;
                uint spawnStart = (uint)_SpawnStart;
                uint spawnCount = (uint)_SpawnCount;

                for (uint i = 0; i < spawnCount; i++)
                {
                    uint spawnIndex = (spawnStart + i) % capacity;

                    if (spawnIndex == index)
                        return true;
                }

                return false;
            }

            float4 CreateSpawnVelocity(uint index)
            {
                float angle = index * 2.39996323;
                float horizontal = cos(angle) * 0.8;
                float depth = sin(angle) * 0.8;
                float vertical = 1.5 + (index % 8) * 0.1;
                float lifetime = 2.0 + (index % 16) * 0.1;

                return float4(
                    horizontal,
                    vertical,
                    depth,
                    lifetime
                );
            }

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float2 sourceUV = FluxGetSourceUV(index);

                float4 velocity = tex2D(_MainTex, sourceUV);
                float4 position = tex2D(_PositionTex, sourceUV);

                bool isActive = velocity.w > 0;

                if (!isActive)
                {
                    if (ShouldSpawn(index))
                        return CreateSpawnVelocity(index);

                    return 0;
                }

                if (position.w >= velocity.w)
                    return 0;

                velocity.xyz += _Gravity.xyz * _DeltaTime;

                return velocity;
            }

            ENDHLSL
        }
    }
}