// 빗방울이 수면에 닿은 자리에 퍼지는 물결 고리 파티클용 셰이더. 수평 빌보드의 UV로 고리 두 개를 그린다.
// 파티클 색에 해와 주변광 세기를 곱해 밤에는 함께 어두워진다.
Shader "Custom/RainRippleParticle"
{
    Properties
    {
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.07
        _InnerRing ("Inner Ring Strength", Range(0, 1)) = 0.5
        _SunInfluence ("Sun Influence", Range(0, 2)) = 0.3
        _AmbientInfluence ("Ambient Influence", Range(0, 2)) = 0.8
        _SurfaceOffset ("Surface Offset", Float) = 0.03
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        // 물이 깊이를 쓰므로 수면과 겹쳐 깜빡이지 않게 앞으로 당긴다.
        Offset -1, -1

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define OUTER_RING_RADIUS 0.8
            #define INNER_RING_RADIUS 0.5

            CBUFFER_START(UnityPerMaterial)
                half _RingWidth;
                half _InnerRing;
                half _SunInfluence;
                half _AmbientInfluence;
                float _SurfaceOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz) + float3(0.0, _SurfaceOffset, 0.0);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.color = input.color;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half Ring(float dist, float radius)
            {
                float x = (dist - radius) / _RingWidth;
                return exp(-x * x);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float dist = length(input.uv * 2.0 - 1.0);
                half ring = Ring(dist, OUTER_RING_RADIUS) + Ring(dist, INNER_RING_RADIUS) * _InnerRing;
                // 사각형 가장자리에서 잘린 티가 나지 않게 바깥을 지운다.
                ring *= saturate((1.0 - dist) * 8.0);

                half3 lighting = GetMainLight().color * _SunInfluence + SampleSH(half3(0.0, 1.0, 0.0)) * _AmbientInfluence;
                half3 color = MixFog(input.color.rgb * lighting, input.fogFactor);
                return half4(color, input.color.a * ring);
            }
            ENDHLSL
        }
    }
}
