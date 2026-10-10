// 하늘 너머에 떠 있는 거대 ALKAGI의 몸체. 뒤쪽 하늘색 안개에 대부분 묻히고 아래로 갈수록 수평선 안개 속으로 사라진다.
// 반투명이어도 겹친 부품이 비치지 않도록 앞면 깊이만 먼저 쓰고 색은 그 위에 한 번만 섞는다.
Shader "Custom/SkyAlkagiApparition"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.12, 0.16, 0.21, 1)
        _HazeTint ("Haze Tint", Color) = (1, 1, 1, 1)
        _Haze ("Haze", Range(0, 1)) = 0.72
        _Alpha ("Alpha", Range(0, 1)) = 0.9
        _FadeHeight ("Fade Height", Float) = 120
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        [HideInInspector] _Emission ("Emission", Float) = 0
        [HideInInspector] _WaterY ("Water Y", Float) = 0
    }

    SubShader
    {
        // 스카이박스 다음, 구름층보다 먼저 그려 구름이 몸체를 덮게 한다.
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-450" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _HazeTint;
            half _Haze;
            half _Alpha;
            float _FadeHeight;
            half4 _EmissionColor;
            half _Emission;
            float _WaterY;
        CBUFFER_END

        // 수면에서 _FadeHeight까지 올라가며 0에서 1로 드러나는 정도.
        half HeightFade(float positionY)
        {
            half fade = saturate((positionY - _WaterY) / max(_FadeHeight, 0.01));
            return fade * fade * (3.0 - 2.0 * fade);
        }
        ENDHLSL

        Pass
        {
            // 거의 사라진 아래쪽은 깊이를 쓰지 않아 뒤의 구름을 막지 않는다.
            Name "DepthPrepass"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float positionY : TEXCOORD0;
            };

            DepthVaryings DepthVert(float4 positionOS : POSITION)
            {
                DepthVaryings output;
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionY = positionWS.y;
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                clip(HeightFade(input.positionY) * _Alpha - 0.3);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.positionWS - GetCameraPositionWS());
                half3 normalWS = normalize(input.normalWS);

                // 몸체 뒤쪽 하늘에서 오는 주변광을 안개 색으로 써서 날씨와 밤낮에 맞춘다.
                half3 sky = SampleSH(viewDir) * _HazeTint.rgb;
                Light mainLight = GetMainLight();
                half3 lit = _BaseColor.rgb * (mainLight.color * saturate(dot(normalWS, mainLight.direction)) * 0.6 + SampleSH(normalWS));
                half fade = HeightFade(input.positionWS.y);
                half3 color = lerp(lit, sky, lerp(1.0, _Haze, fade));
                color += _EmissionColor.rgb * _Emission;
                return half4(color, _Alpha * fade);
            }
            ENDHLSL
        }
    }
}
