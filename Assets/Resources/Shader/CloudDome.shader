// 카메라를 감싸는 큰 구 안쪽에 그리는 구름층. 하늘 위에 반투명으로 덮어 흐린 날을 만든다.
// 덮임 정도는 EnvironmentController가 머티리얼의 _Coverage로 넘기며, 해와 주변광 세기를 받아 밤에는 함께 어두워진다.
// 하늘 ALKAGI가 있으면 SkyAlkagiClouds가 그 주변에 구름을 모으고, 눈 충전 빛과 레이저를 따라 걷히는 범위를 넘긴다.
Shader "Custom/CloudDome"
{
    Properties
    {
        _Coverage ("Coverage", Range(0, 1)) = 0
        _MaxOpacity ("Max Opacity", Range(0, 1)) = 0.95
        _CloudScale ("Cloud Scale", Float) = 0.8
        _CloudSoftness ("Cloud Softness", Range(0.01, 0.5)) = 0.18
        _WindSpeed ("Wind Speed", Vector) = (0.01, 0.004, 0, 0)
        _LitColor ("Lit Color", Color) = (0.74, 0.8, 0.9, 1)
        _ShadowColor ("Shadow Color", Color) = (0.25, 0.29, 0.37, 1)
        _SunInfluence ("Sun Influence", Range(0, 2)) = 0.35
        _AmbientInfluence ("Ambient Influence", Range(0, 2)) = 0.8
        _DepthRadius ("Depth Radius", Float) = 600

        [Header(Focus)]
        _FocusCoverage ("Focus Coverage", Range(0, 1)) = 0.75
        _FocusAngle ("Focus Angle Cos", Range(-1, 1)) = 0.4
        _VeilAngle ("Veil Angle Cos", Range(0, 1)) = 0.985
        _Veil ("Veil", Range(0, 1)) = 0.45
        [HDR] _GlowColor ("Glow Color", Color) = (2.4, 0.3, 0.45, 1)

        [Header(Cut)]
        [HDR] _CutColor ("Cut Color", Color) = (2, 0.2, 0.12, 1)
        _CutWidth ("Cut Width (sin)", Range(0.001, 0.1)) = 0.004

        [HideInInspector] _FocusWeight ("Focus Weight", Float) = 0
        [HideInInspector] _FocusDir ("Focus Dir", Vector) = (0, 0, 1, 0)
        [HideInInspector] _Glow ("Glow", Float) = 0
        [HideInInspector] _CutAmount ("Cut Amount", Float) = 0
        [HideInInspector] _CutGlow ("Cut Glow", Float) = 0
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
            // 카메라가 내려다봐서 보이는 하늘이 낮으므로 꽤 낮은 곳부터 무늬를 살린다.
            #define HORIZON_BLEND 10.0
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
                float _DepthRadius;
                half _FocusCoverage;
                half _FocusAngle;
                half _VeilAngle;
                half _Veil;
                half4 _GlowColor;
                half4 _CutColor;
                float _CutWidth;
                half _FocusWeight;
                float4 _FocusDir;
                half _Glow;
                half _CutAmount;
                half _CutGlow;
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
                // 구 크기와 관계없이 _DepthRadius 거리에 그려, 그보다 먼 하늘 ALKAGI 몸체를 구름이 덮게 한다.
                float3 cameraWS = GetCameraPositionWS();
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = cameraWS + normalize(positionWS - cameraWS) * _DepthRadius;
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

                // 하늘 ALKAGI 쪽은 비가 오지 않아도 구름이 끼고, 몸체 바로 앞은 얇은 막만 남겨 윤곽이 비치게 한다.
                float toFocus = dot(dir, _FocusDir.xyz);
                half focus = smoothstep(_FocusAngle, lerp(_FocusAngle, 1.0, 0.5), toFocus) * _FocusWeight;
                half veil = smoothstep(_VeilAngle, lerp(_VeilAngle, 1.0, 0.6), toFocus) * _FocusWeight;
                half coverage = max(_Coverage, _FocusCoverage * focus);

                // 시선을 구름층 평면에 투영해 무늬 좌표로 쓴다.
                float2 uv = dir.xz / (height + HORIZON_LIFT) * _CloudScale + _Time.y * _WindSpeed.xy;
                float noise = Fbm(uv);
                float threshold = lerp(0.9, 0.1, coverage);
                float density = smoothstep(threshold - _CloudSoftness, threshold + _CloudSoftness, noise);
                density = lerp(coverage, density, saturate(height * HORIZON_BLEND));
                // 수평선 아래는 물과 지형이 가리므로 그리지 않는다.
                density *= saturate(dir.y * 20.0 + 1.0);
                density *= lerp(1.0, _Veil, veil);

                // ALKAGI 쪽 수평선에서 돔 꼭대기를 넘어 반대편 수평선까지 이어지는 세로 큰 원을 따라 한 줄로 가른다.
                // 선까지의 거리는 세로면과의 각도 사인값이라 돔 어디서나 같은 폭이다.
                float2 cutAxis = normalize(_FocusDir.xz);
                float cutDistance = abs(dir.x * cutAxis.y - dir.z * cutAxis.x);
                // 벌어지는 속도는 SkyAlkagiClouds가 커브로 정한 너비 배율 _CutAmount로 받는다.
                float cutWidth = max(_CutWidth * _CutAmount, 0.0001);
                half cutMask = step(0.001, _CutAmount);
                half cut = (1.0 - smoothstep(cutWidth * 0.5, cutWidth, cutDistance)) * cutMask;
                // 빛나는 경계는 벌어진 폭과 관계없이 Cut Width만큼만 얇게 남는다.
                half edge = saturate(1.0 - abs(cutDistance - cutWidth * 0.75) / _CutWidth) * cutMask;

                // 두꺼운 곳일수록 아랫면이 어둡다.
                half thickness = saturate((noise - threshold) * 2.5);
                half3 color = lerp(_LitColor.rgb, _ShadowColor.rgb, thickness);
                Light mainLight = GetMainLight();
                color *= mainLight.color * _SunInfluence + SampleSH(half3(0.0, 1.0, 0.0)) * _AmbientInfluence;

                // 눈이 충전되면 ALKAGI 가까운 구름 속이 붉게 깜빡이고, 갈라진 경계는 옅게 빛나다 다 열리면 사라진다.
                half nearFocus = pow(saturate((toFocus - _FocusAngle) / (1.0 - _FocusAngle)), 4.0) * _FocusWeight;
                half flicker = 0.7 + 0.3 * sin(_Time.y * 23.0 + noise * 15.0);
                color += _GlowColor.rgb * (_Glow * nearFocus * flicker);
                color += _CutColor.rgb * (edge * _CutGlow);
                return half4(color, density * (1.0 - cut) * _MaxOpacity);
            }
            ENDHLSL
        }
    }
}
