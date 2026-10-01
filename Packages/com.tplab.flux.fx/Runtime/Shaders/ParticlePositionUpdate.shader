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
            float _SimulationSpace;
            float4 _SystemPosition;
            float4 _SystemRotation;
            float4 _SystemScale;
            float _DeltaTime;

            float3 RotateVector(float3 value, float4 rotation)
            {
                return value + 2.0 * cross(rotation.xyz, cross(rotation.xyz, value) + rotation.w * value);
            }

            float3 CreateShapeDirection(uint index)
            {
                if (_ShapeType > 2.5)
                {
                    float angle = ((float)index / (float)_FluxDestinationCount) * UNITY_TWO_PI;

                    return float3(cos(angle), 0, sin(angle));
                }

                float3 direction = FluxFXCreateSpawnDirection(
                    index,
                    (uint)_FluxDestinationCount
                );

                if (_ShapeType > 1.5)
                {
                    direction.y = abs(direction.y);
                }

                return direction;
            }

            float3 CreateSpawnPosition(uint index)
            {
                if (_ShapeType < 0.5)
                {
                    if (_SimulationSpace > 0.5)
                    {
                        return _SystemPosition.xyz;
                    }

                    return 0;
                }

                float3 position = CreateShapeDirection(index) * _ShapeRadius;

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
                    return float4(CreateSpawnPosition(index), 0);

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