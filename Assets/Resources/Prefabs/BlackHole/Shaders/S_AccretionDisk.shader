Shader "BlackHole/AccretionDisk"
{
    Properties
    {
        [HDR] _InnerColor("Inner Color", Color) = (1, 0.78, 0.35, 1)
        [HDR] _OuterColor("Outer Color", Color) = (1, 0.12, 0.015, 1)
        _Intensity("Emission Intensity", Range(0, 10)) = 3
        _Opacity("Opacity", Range(0, 1)) = 1
        _InnerRadius("Inner Radius (Local)", Range(0, 0.49)) = 0
        _OuterRadius("Outer Radius (Local)", Range(0.01, 0.5)) = 0.49
        _EdgeSoftness("Edge Softness", Range(0.001, 0.1)) = 0.025
        _RotationSpeed("Rotation Speed", Float) = 0.35
        _NoiseScale("Noise Scale", Range(1, 40)) = 16
        _SpiralStrength("Spiral Strength", Range(0, 20)) = 8
        _HoleRadius("Occluding Hole Radius (Local)", Range(0, 0.5)) = 0.125
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "AccretionDisk"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "AccretionDisk.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _InnerColor;
                float4 _OuterColor;
                float _Intensity;
                float _Opacity;
                float _InnerRadius;
                float _OuterRadius;
                float _EdgeSoftness;
                float _RotationSpeed;
                float _NoiseScale;
                float _SpiralStrength;
                float _HoleRadius;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 positionOS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// <summary>
            /// 로컬 정점 위치를 클립 및 월드 좌표로 변환하여 원반 프래그먼트 입력을 반환한다.
            /// </summary>
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.positionOS = input.positionOS.xy;
                return output;
            }

            /// <summary>
            /// 원반 위치와 시간을 사용하여 회전하는 발광 색상을 반환하고 중심 뒤의 원반을 제외한다.
            /// </summary>
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                AccretionDiskSettings settings;
                settings.innerColor = _InnerColor.rgb;
                settings.outerColor = _OuterColor.rgb;
                settings.intensity = _Intensity;
                settings.opacity = _Opacity;
                settings.innerRadius = _InnerRadius;
                settings.outerRadius = _OuterRadius;
                settings.edgeSoftness = _EdgeSoftness;
                settings.rotationSpeed = _RotationSpeed;
                settings.noiseScale = _NoiseScale;
                settings.spiralStrength = _SpiralStrength;
                float4 disk = EvaluateAccretionDisk(input.positionOS, settings);
                clip(disk.a - 0.001);

                // 중심 구체보다 뒤에 있는 원반만 가려 앞쪽 원반은 중심을 지나 보이게 한다.
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float holeRadiusWS = _HoleRadius * length(TransformObjectToWorldDir(float3(1, 0, 0), false));
                float3 cameraToCenter = centerWS - _WorldSpaceCameraPos;
                float3 cameraToDisk = input.positionWS - _WorldSpaceCameraPos;
                float diskDistance = length(cameraToDisk);
                float3 rayDirection = cameraToDisk / max(diskDistance, 0.0001);
                float closestDistance = dot(cameraToCenter, rayDirection);
                float discriminant = holeRadiusWS * holeRadiusWS
                    - (dot(cameraToCenter, cameraToCenter) - closestDistance * closestDistance);
                if (_HoleRadius > 0.0 && discriminant >= 0.0)
                {
                    float nearDistance = closestDistance - sqrt(discriminant);
                    if (nearDistance > 0.0 && nearDistance < diskDistance)
                        discard;
                }

                return half4(disk);
            }
            ENDHLSL
        }
    }
}
