Shader "Hidden/FluxFX/Trail"
{
    Properties
    {
        _TrailFirst ("First", 2D) = "black" {}
        _TrailSecond ("Second", 2D) = "black" {}
        _TrailMetadata ("Generation", 2D) = "black" {}
        _TrailEnabled ("Enabled", Float) = 1
        _TrailDuration ("Duration", Float) = 0.5
        _TrailWidth ("Width", Float) = 0.05
        _TrailColor ("Color", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _TrailFirst, _TrailSecond, _TrailMetadata;
            float4 _TrailSize, _TrailAges, _TrailColor;
            float _TrailWidth, _TrailEnabled, _TrailDuration;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 trail : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float alpha : TEXCOORD0;
                float valid : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float2 uv = (float2(fmod(v.trail.x, _TrailSize.x), floor(v.trail.x / _TrailSize.x)) + 0.5) / _TrailSize.xy;
                float4 a = tex2Dlod(_TrailFirst, float4(uv,0,0));
                float4 b = tex2Dlod(_TrailSecond, float4(uv,0,0));
                float4 meta = tex2Dlod(_TrailMetadata, float4(uv,0,0));
                float firstAge = _TrailAges.x;
                float secondAge = _TrailAges.y;
                if (firstAge > _TrailDuration && firstAge > secondAge)
                {
                    float fraction = saturate((firstAge - _TrailDuration) / (firstAge - secondAge));
                    a.xyz = lerp(a.xyz, b.xyz, fraction);
                    firstAge = _TrailDuration;
                }
                float3 tangent = b.xyz - a.xyz;
                float lengthSquared = dot(tangent, tangent);
                float3 position = lerp(a.xyz, b.xyz, v.uv.x);
                float3 side = cross(tangent, _WorldSpaceCameraPos - position);
                float sideLength = dot(side, side);
                side = sideLength > 1e-12 ? side * rsqrt(max(sideLength, 1e-12)) : UNITY_MATRIX_I_V._m00_m10_m20;
                float fade = saturate(1 - lerp(firstAge, secondAge, v.uv.x) / _TrailDuration);
                position += side * (v.uv.y - 0.5) * _TrailWidth * fade;
                o.vertex = UnityWorldToClipPos(position);
                o.alpha = fade;
                o.valid = _TrailEnabled > 0.5 && v.trail.x < _TrailSize.z && meta.y > 0.5 &&
                    a.w >= 0 && a.w == meta.x && b.w == meta.x && lengthSquared > 1e-12 &&
                    _TrailAges.x >= 0 && secondAge >= 0 && secondAge < _TrailDuration;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                clip(i.valid - 0.5);
                return float4(_TrailColor.rgb, _TrailColor.a * i.alpha);
            }
            ENDHLSL
        }
    }
}
