// 마인크래프트처럼 픽셀 단위로 흐르는 물 셰이더. 텍스처 없이 면마다 UV를 픽셀 격자로 나눠 색을 뽑는다.
// 무늬는 UV y 방향으로 한 칸씩 흘러간다. 큐브 옆면은 UV y가 위쪽이라 흐름 속도가 양수면 아래로 흐른다.
Shader "Custom/FlowingWater"
{
    Properties
    {
        _LightColor ("Light Color", Color) = (0.32, 0.55, 1, 1)
        _DarkColor ("Dark Color", Color) = (0.1, 0.25, 0.8, 1)
        _Pixels ("Pixels Per Face", Float) = 16
        _FlowSpeed ("Flow Speed (pixels/sec)", Float) = 12
        _StreakLength ("Streak Length (pixels)", Float) = 4
        _ShadeMin ("Shade Min", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _LightColor;
            half4 _DarkColor;
            float _Pixels;
            float _FlowSpeed;
            float _StreakLength;
            half _ShadeMin;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            // 칸 좌표마다 0~1 사이의 고정된 난수를 만든다.
            float Hash(float2 cell)
            {
                return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 흐름도 한 칸씩 끊어 움직여 마크 텍스처 애니메이션처럼 보이게 한다.
                float flowOffset = floor(_Time.y * _FlowSpeed);
                float2 cell = floor(input.uv * _Pixels + float2(0.0, flowOffset));
                float streak = Hash(float2(cell.x, floor(cell.y / _StreakLength)));
                float speckle = Hash(cell);
                half3 color = lerp(_DarkColor.rgb, _LightColor.rgb, streak * 0.7 + speckle * 0.3);

                // 면 방향에 따라 밝기를 달리해 블록 모서리가 구분되게 한다.
                Light mainLight = GetMainLight();
                half facing = saturate(dot(normalize(input.normalWS), mainLight.direction) * 0.5 + 0.5);
                color *= lerp(_ShadeMin, 1.0, facing) * mainLight.color;
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
