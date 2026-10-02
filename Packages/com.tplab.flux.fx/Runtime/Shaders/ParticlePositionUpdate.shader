Shader "FluxFX/ParticlePositionUpdate"
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
            #include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

            sampler2D_float _MainTex;
            sampler2D_float _VelocityTex;
            sampler2D_float _CurrentVelocityTex;

            float _ShapeType;
            float _ShapeRadius;
            float4 _ShapeSize;
            float _SimulationSpace;
            float4 _SystemPosition;
            float4 _SystemRotation;
            float4 _SystemScale;
            float _DeltaTime;
            float _SpawnStart;
            float _SpawnCount;

            float3 RotateVector(float3 value, float4 rotation)
            {
                return value + 2.0 * cross(rotation.xyz, cross(rotation.xyz, value) + rotation.w * value);
            }

            float3 CreateShapeDirection(uint spawnSequence)
            {
                if (_ShapeType > 2.5 && _ShapeType < 3.5)
                {
                    float angle = spawnSequence * 2.39996323;

                    return float3(cos(angle), 0, sin(angle));
                }

                float3 direction = FluxFXCreateSpawnDirection(spawnSequence);

                if (_ShapeType > 1.5 && _ShapeType < 2.5)
                {
                    direction.y = abs(direction.y);
                }

                return direction;
            }

            float3 CreateBoxSpawnPosition(uint spawnSequence)
            {
                uint face = spawnSequence % 6u;
                float u = FluxFXRandom01(spawnSequence * 2u) * 2.0 - 1.0;
                float v = FluxFXRandom01(spawnSequence * 2u + 1u) * 2.0 - 1.0;
                float3 halfSize = _ShapeSize.xyz * 0.5;

                if (face == 0u)
                    return float3(halfSize.x, u * halfSize.y, v * halfSize.z);

                if (face == 1u)
                    return float3(-halfSize.x, u * halfSize.y, v * halfSize.z);

                if (face == 2u)
                    return float3(u * halfSize.x, halfSize.y, v * halfSize.z);

                if (face == 3u)
                    return float3(u * halfSize.x, -halfSize.y, v * halfSize.z);

                if (face == 4u)
                    return float3(u * halfSize.x, v * halfSize.y, halfSize.z);

                return float3(u * halfSize.x, v * halfSize.y, -halfSize.z);
            }

            float3 CreateSpawnPosition(uint spawnSequence)
            {
                if (_ShapeType < 0.5)
                {
                    if (_SimulationSpace > 0.5)
                    {
                        return _SystemPosition.xyz;
                    }

                    return 0;
                }

                float3 position;

                if (_ShapeType > 3.5)
                {
                    position = CreateBoxSpawnPosition(spawnSequence);
                }
                else
                {
                    position = CreateShapeDirection(spawnSequence) * _ShapeRadius;
                }

                if (_SimulationSpace > 0.5)
                {
                    position *= _SystemScale.xyz;
                    position = RotateVector(position, _SystemRotation);
                    position += _SystemPosition.xyz;
                }

                return position;
            }

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
                {
                    uint spawnSequence = FluxFXGetSpawnSequence(
                        index,
                        (uint)_FluxDestinationCount,
                        (uint)_SpawnStart,
                        (uint)_SpawnCount);

                    return float4(CreateSpawnPosition(spawnSequence), 0);
                }

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