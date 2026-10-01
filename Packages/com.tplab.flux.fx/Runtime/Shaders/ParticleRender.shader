Shader "FluxFX/ParticleRender"
{
    Properties
    {
        _PositionTex ("Position", 2D) = "black" {}
        _VelocityTex ("Velocity", 2D) = "black" {}
        _VisualTex ("Visual", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"

            sampler2D_float _PositionTex;
            sampler2D_float _VelocityTex;
            sampler2D_float _VisualTex;

            float4 _EndColor;
            float3 _StartRotation;
            float _EndSize;
            float _ColorOverLifetimeEnabled;
            float _SizeOverLifetimeEnabled;
            float _SimulationSpace;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 particleData : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            float3 RotateX(float3 position, float angle)
            {
                float s;
                float c;
                sincos(angle, s, c);

                return float3(
                    position.x,
                    position.y * c - position.z * s,
                    position.y * s + position.z * c
                );
            }

            float3 RotateY(float3 position, float angle)
            {
                float s;
                float c;
                sincos(angle, s, c);

                return float3(
                    position.x * c + position.z * s,
                    position.y,
                    -position.x * s + position.z * c
                );
            }

            float3 RotateZ(float3 position, float angle)
            {
                float s;
                float c;
                sincos(angle, s, c);

                return float3(
                    position.x * c - position.y * s,
                    position.x * s + position.y * c,
                    position.z
                );
            }

            float3 RotateEuler(float3 position, float3 rotation)
            {
                position = RotateX(position, rotation.x);
                position = RotateY(position, rotation.y);
                position = RotateZ(position, rotation.z);

                return position;
            }

            v2f vert(appdata v)
            {
                v2f o;

                uint particleIndex = (uint)v.particleData.x;
                float2 stateUV = FluxGetSourceUV(particleIndex);

                float4 position = tex2Dlod(_PositionTex, float4(stateUV, 0, 0));
                float4 velocity = tex2Dlod(_VelocityTex, float4(stateUV, 0, 0));
                float4 visual = tex2Dlod(_VisualTex, float4(stateUV, 0, 0));

                float age = position.w;
                float lifetime = velocity.w;

                if (lifetime <= 0 || age >= lifetime)
                {
                    o.vertex = float4(2, 2, 2, 1);
                    o.color = 0;
                    return o;
                }

                float normalizedAge = saturate(age / lifetime);
                float3 startColor = visual.rgb;
                float startSize = visual.a;
                float3 color = startColor;
                float size = startSize;

                if (_ColorOverLifetimeEnabled > 0.5)
                {
                    color = lerp(startColor, _EndColor.rgb, normalizedAge);
                }

                if (_SizeOverLifetimeEnabled > 0.5)
                {
                    size = lerp(startSize, _EndSize, normalizedAge);
                }

                float3 rotation = radians(_StartRotation);
                float3 particleVertex = RotateEuler(v.vertex.xyz * size, rotation);
                if (_SimulationSpace > 0.5)
                {
                    float3 worldPosition = position.xyz + particleVertex;
                    o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1));
                }
                else
                {
                    float3 localPosition = position.xyz + particleVertex;
                    o.vertex = UnityObjectToClipPos(float4(localPosition, 1));
                }
                o.color = float4(color, 1);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}