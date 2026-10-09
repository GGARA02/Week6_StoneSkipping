// 부스트 점프대 경사면용 셰이더. 텍스처 없이 UV로 경사면 위쪽을 가리키는 화살표 줄무늬를 그린다.
// UV는 x가 경사면 폭, y가 경사면 길이 방향이다. 색은 길이를 따라 무지개로 바뀌며 시간에 따라 흐른다.
Shader "Custom/BoostRainbow"
{
    Properties
    {
        [Header(Rainbow)]
        _HueSpeed ("Hue Speed", Float) = 0.35
        _HueSpread ("Hue Spread", Float) = 1
        _Saturation ("Saturation", Range(0, 1)) = 0.9

        [Header(Chevron)]
        _ChevronCount ("Chevron Count", Float) = 4
        _ChevronSlant ("Chevron Slant", Float) = 0.7
        _ChevronWidth ("Chevron Width", Range(0.05, 0.95)) = 0.45
        _ScrollSpeed ("Scroll Speed", Float) = 1.5

        [Header(Brightness)]
        _BackgroundIntensity ("Background Intensity", Float) = 0.35
        _ChevronIntensity ("Chevron Intensity", Float) = 2.5
        _CoreWhiten ("Chevron Core Whiten", Range(0, 1)) = 0.3
        _EdgeGlow ("Edge Glow", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _HueSpeed;
            float _HueSpread;
            half _Saturation;
            float _ChevronCount;
            float _ChevronSlant;
            float _ChevronWidth;
            float _ScrollSpeed;
            half _BackgroundIntensity;
            half _ChevronIntensity;
            half _CoreWhiten;
            half _EdgeGlow;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            // 색상환 위치(0~1)를 채도 1, 명도 1인 RGB로 바꾼다.
            half3 HueToRgb(half hue)
            {
                return saturate(abs(frac(hue + half3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 가운데가 0, 양옆 끝이 1이다. 가운데가 앞서 나가도록 더해 위쪽을 가리키는 화살표 모양을 만든다.
                float across = abs(input.uv.x - 0.5) * 2.0;
                float phase = input.uv.y * _ChevronCount + across * _ChevronSlant - _Time.y * _ScrollSpeed;
                float stripe = frac(phase);
                float edge = fwidth(phase) * 1.5;
                half chevron = smoothstep(0.0, edge, stripe) * (1.0 - smoothstep(_ChevronWidth - edge, _ChevronWidth, stripe));

                half hue = frac(input.uv.y * _HueSpread - _Time.y * _HueSpeed);
                half3 rainbow = lerp(1.0, HueToRgb(hue), _Saturation);
                half3 background = rainbow * _BackgroundIntensity;
                half3 arrow = lerp(rainbow, 1.0, _CoreWhiten) * _ChevronIntensity;
                half3 color = lerp(background, arrow, chevron);

                // 난간 쪽 가장자리를 밝혀 빛 띠처럼 보이게 한다.
                color += rainbow * pow(across, 8.0) * _EdgeGlow;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            DepthNormalsVaryings DepthNormalsVert(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(DepthNormalsVaryings input) : SV_Target
            {
                return half4(normalize(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
