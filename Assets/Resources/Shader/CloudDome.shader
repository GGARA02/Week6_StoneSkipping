// 카메라를 감싸는 큰 구 안쪽에 그리는 구름층. 하늘 위에 반투명으로 덮어 흐린 날을 만든다.
// 덮임 정도는 EnvironmentController가 머티리얼의 _Coverage로 넘기며, 해와 주변광 세기를 받아 밤에는 함께 어두워진다.
Shader "Custom/CloudDome"
{
    Properties
    {
        _Coverage ("Coverage", Range(0, 1)) = 0
        _MaxOpacity ("Max Opacity", Range(0, 1)) = 0.95
        _CloudScale ("Cloud Scale", Float) = 0.8
        _CloudSoftness ("Cloud Softness", Range(0.01, 0.5)) = 0.15
        _WindSpeed ("Wind Speed", Vector) = (0.02, 0.008, 0, 0)
        _LitColor ("Lit Color", Color) = (0.86, 0.88, 0.92, 1)
        _ShadowColor ("Shadow Color", Color) = (0.48, 0.51, 0.56, 1)
        _SunInfluence ("Sun Influence", Range(0, 2)) = 0.35
        _AmbientInfluence ("Ambient Influence", Range(0, 2)) = 0.8
    }

    SubShader
    {
        // 하늘 다음, 물보다 먼저 그린다. 물 아래로는 그리지 않는다.
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-400" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define FBM_OCTAVES 5
            // 지평선 근처는 구름층 투영이 크게 늘어나서 무늬 대신 평균 덮임으로 채운다.
            #define HORIZON_BLEND 5.0
            #define HORIZON_LIFT 0.12

            CBUFFER_START(UnityPerMaterial)
                half _Coverage;
                half _MaxOpacity;
                float _CloudScale;
                half _CloudSoftness;
                float4 _WindSpeed;
                half4 _LitColor;
                half4 _ShadowColor;
                half _SunInfluence;
                half _AmbientInfluence;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            // 격자 좌표 해시. sin을 쓰지 않아 큰 좌표에서도 패턴이 깨지지 않는다.
            float Hash12(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash12(cell);
                float b = Hash12(cell + float2(1.0, 0.0));
                float c = Hash12(cell + float2(0.0, 1.0));
                float d = Hash12(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;
                [unroll]
                for (int i = 0; i < FBM_OCTAVES; i++)
                {
                    sum += ValueNoise(p) * amplitude;
                    p = p * 2.03 + float2(17.3, 9.1);
                    amplitude *= 0.5;
                }
                return sum / (1.0 - pow(0.5, FBM_OCTAVES));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.positionWS - GetCameraPositionWS());
                float height = max(dir.y, 0.0);

                // 시선을 구름층 평면에 투영해 무늬 좌표로 쓴다.
                float2 uv = dir.xz / (height + HORIZON_LIFT) * _CloudScale + _Time.y * _WindSpeed.xy;
                float noise = Fbm(uv);
                float threshold = lerp(0.9, 0.1, _Coverage);
                float density = smoothstep(threshold - _CloudSoftness, threshold + _CloudSoftness, noise);
                density = lerp(_Coverage, density, saturate(height * HORIZON_BLEND));
                // 수평선 아래는 물과 지형이 가리므로 그리지 않는다.
                density *= saturate(dir.y * 20.0 + 1.0);

                // 두꺼운 곳일수록 아랫면이 어둡다.
                half thickness = saturate((noise - threshold) * 2.5);
                half3 color = lerp(_LitColor.rgb, _ShadowColor.rgb, thickness);
                Light mainLight = GetMainLight();
                half3 lighting = mainLight.color * _SunInfluence + SampleSH(half3(0.0, 1.0, 0.0)) * _AmbientInfluence;
                return half4(color * lighting, density * _MaxOpacity);
            }
            ENDHLSL
        }
    }
}
