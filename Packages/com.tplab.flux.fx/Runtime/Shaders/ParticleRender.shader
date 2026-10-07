Shader "FluxFX/ParticleRender"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PositionTex ("Position", 2D) = "black" {}
        _VelocityTex ("Velocity", 2D) = "black" {}
        _VisualTex ("Visual", 2D) = "white" {}
        _RotationTex ("Rotation", 2D) = "black" {}
        _ColorOverLifetimeLut ("Color over Lifetime LUT", 2D) = "white" {}
        _SizeOverLifetimeLut ("Size over Lifetime LUT", 2D) = "white" {}
        _RotationOverLifetimeLut ("Rotation over Lifetime LUT", 2D) = "white" {}

        _TextureSheetAnimationEnabled ("Texture Sheet Animation Enabled", Float) = 0
        _TextureSheetTilesX ("Tiles X", Float) = 1
        _TextureSheetTilesY ("Tiles Y", Float) = 1
        _TextureSheetCycles ("Cycles", Float) = 1
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

            sampler2D _MainTex;
            sampler2D_float _PositionTex;
            sampler2D_float _VelocityTex;
            sampler2D_float _VisualTex;
            sampler2D_float _RotationTex;
            sampler2D _ColorOverLifetimeLut;
            sampler2D _SizeOverLifetimeLut;
            sampler2D _RotationOverLifetimeLut;

            float4 _EndColor;
            float _EndSize;
            float3 _EndRotation;
            float _ColorOverLifetimeEnabled;
            float _SizeOverLifetimeEnabled;
            float _ColorOverLifetimeMode;
            float _SizeOverLifetimeMode;
            float _RotationOverLifetimeEnabled;
            float _RotationOverLifetimeMode;
            float _TextureSheetAnimationEnabled;
            float _TextureSheetTilesX;
            float _TextureSheetTilesY;
            float _TextureSheetCycles;
            float _SimulationSpace;
            float _RenderMode;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 particleData : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
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

            float3 GetBillboardVertex(float2 vertex, float size)
            {
                float3 cameraRight = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 cameraUp = UNITY_MATRIX_I_V._m01_m11_m21;

                return (cameraRight * vertex.x + cameraUp * vertex.y) * size;
            }

            float2 Rotate2D(float2 value, float angle)
            {
                float s;
                float c;
                sincos(angle, s, c);

                return float2(
                    value.x * c - value.y * s,
                    value.x * s + value.y * c
                );
            }

            float3 GetRotationOverLifetime(float normalizedAge)
            {
                if (_RotationOverLifetimeEnabled <= 0.5)
                    return 0;

                if (_RotationOverLifetimeMode > 0.5)
                {
                    return _EndRotation
                        * tex2Dlod(
                            _RotationOverLifetimeLut,
                            float4(normalizedAge, 0.5, 0, 0)).r;
                }

                return _EndRotation * normalizedAge;
            }

            float2 GetTextureSheetUV(float2 uv, float normalizedAge)
            {
                if (_TextureSheetAnimationEnabled <= 0.5)
                    return uv;

                float tilesX = max(1, floor(_TextureSheetTilesX));
                float tilesY = max(1, floor(_TextureSheetTilesY));
                float frameCount = tilesX * tilesY;

                float animationTime = frac(normalizedAge * max(0, _TextureSheetCycles));
                float frame = min(floor(animationTime * frameCount), frameCount - 1);

                float column = fmod(frame, tilesX);
                float row = floor(frame / tilesX);

                float2 tileSize = 1.0 / float2(tilesX, tilesY);
                float2 tileOffset = float2(
                    column * tileSize.x,
                    (tilesY - row - 1) * tileSize.y);

                return uv * tileSize + tileOffset;
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
                    o.uv = 0;
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
                    if (_ColorOverLifetimeMode > 0.5)
                    {
                        color = tex2Dlod(_ColorOverLifetimeLut, float4(normalizedAge, 0.5, 0, 0)).rgb;
                    }
                    else
                    {
                        color = lerp(startColor, _EndColor.rgb, normalizedAge);
                    }
                }

                if (_SizeOverLifetimeEnabled > 0.5)
                {
                    if (_SizeOverLifetimeMode > 0.5)
                    {
                        size = startSize * tex2Dlod(_SizeOverLifetimeLut, float4(normalizedAge, 0.5, 0, 0)).r;
                    }
                    else
                    {
                        size = lerp(startSize, _EndSize, normalizedAge);
                    }
                }

                float3 rotationOverLifetime = GetRotationOverLifetime(normalizedAge);

                float3 particleVertex;
                if (_RenderMode < 0.5)
                {
                    particleVertex = GetBillboardVertex(
                        Rotate2D(v.vertex.xy, radians(rotationOverLifetime.z)),
                        size);
                }
                else
                {
                    float3 startRotation = tex2Dlod(_RotationTex, float4(stateUV, 0, 0)).xyz;
                    float3 rotation = radians(startRotation + rotationOverLifetime);

                    particleVertex = RotateEuler(v.vertex.xyz * size, rotation);
                }

                if (_SimulationSpace > 0.5)
                {
                    float3 worldPosition = position.xyz + particleVertex;
                    o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1));
                }
                else
                {
                    if (_RenderMode < 0.5)
                    {
                        float3 worldPosition = mul(unity_ObjectToWorld, float4(position.xyz, 1)).xyz + particleVertex;
                        o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1));
                    }
                    else
                    {
                        float3 localPosition = position.xyz + particleVertex;
                        o.vertex = UnityObjectToClipPos(float4(localPosition, 1));
                    }
                }

                o.uv = GetTextureSheetUV(v.uv, normalizedAge);
                o.color = float4(color, 1);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDHLSL
        }
    }
}