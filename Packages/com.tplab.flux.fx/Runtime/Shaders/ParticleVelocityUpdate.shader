Shader "FluxFX/ParticleVelocityUpdate"
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
            #include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

            sampler2D_float _MainTex;
            sampler2D_float _PositionTex;

            float4 _Gravity;
            float _Drag;
            float _NoiseStrength;
            float _NoiseScale;
            float _NoiseTime;
            float4 _VortexCenter;
            float4 _VortexAxis;
            float _VortexStrength;
            float _Lifetime;
            float _InitialSpeed;
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
                float3 direction = FluxFXCreateSpawnDirection(
                    index,
                    (uint)_FluxDestinationCount
                );

                return float4(
                    direction * _InitialSpeed,
                    _Lifetime
                );
            }

            float3 EvaluateNoise(float3 position)
            {
                float3 p = position * _NoiseScale;

                float3 noise;

                noise.x = sin(p.y * 1.37 + p.z * 0.73 + _NoiseTime);
                noise.y = sin(p.z * 1.11 + p.x * 1.53 + _NoiseTime * 1.17);
                noise.z = sin(p.x * 0.91 + p.y * 1.29 + _NoiseTime * 0.83);

                return noise;
            }

            float3 EvaluateVortex(float3 position)
            {
                float3 offset = position - _VortexCenter.xyz;

                float axialDistance = dot(offset, _VortexAxis.xyz);
                float3 radialOffset = offset - _VortexAxis.xyz * axialDistance;

                float radialLength = length(radialOffset);

                if (radialLength <= 0.0001)
                    return 0;

                float3 radialDirection = radialOffset / radialLength;
                float3 tangent = cross(_VortexAxis.xyz, radialDirection);

                return tangent * _VortexStrength;
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

                float3 noise = EvaluateNoise(position.xyz);
                velocity.xyz += noise * _NoiseStrength * _DeltaTime;

                float3 vortex = EvaluateVortex(position.xyz);
                velocity.xyz += vortex * _DeltaTime;

                float dragFactor = exp(-_Drag * _DeltaTime);
                velocity.xyz *= dragFactor;

                return velocity;
            }

            ENDHLSL
        }
    }
}