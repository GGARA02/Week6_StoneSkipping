// 거대 ALKAGI 눈 주변에 번지는 빛. 항상 카메라를 향하는 사각형에 가산으로 그려 구름 너머로도 빛이 번져 보인다.
// 크기와 세기는 SkyAlkagi가 충전 정도에 맞춰 속성 블록으로 넘긴다.
Shader "Custom/SkyAlkagiGlow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.12, 0.18, 1)
        _Falloff ("Falloff", Float) = 5
        _DepthOffset ("Depth Offset", Float) = 40
        [HideInInspector] _Size ("Size", Float) = 50
        [HideInInspector] _Intensity ("Intensity", Float) = 1
    }

    SubShader
    {
        // 구름층 다음에 그려 구름에 덮여도 빛이 남게 한다.
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-380" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Falloff;
                float _DepthOffset;
                float _Size;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 offset : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 centerVS = TransformWorldToView(TransformObjectToWorld(float3(0.0, 0.0, 0.0)));
                // 몸체 깊이에 가려지지 않도록 카메라 쪽으로 당긴다. 뷰 공간에서는 카메라 앞이 -Z다.
                centerVS.z += _DepthOffset;
                output.positionCS = TransformWViewToHClip(centerVS + float3(input.positionOS.xy * _Size, 0.0));
                output.offset = input.positionOS.xy * 2.0;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float distanceSq = dot(input.offset, input.offset);
                half glow = exp(-distanceSq * _Falloff) * saturate(1.0 - distanceSq);
                return half4(_Color.rgb * (_Intensity * glow), 1.0);
            }
            ENDHLSL
        }
    }
}
