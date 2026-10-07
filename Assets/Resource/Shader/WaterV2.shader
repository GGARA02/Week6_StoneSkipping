// 물수제비용 물 셰이더. 여러 방향 파도 노멀, 하늘 반사, 햇빛 반짝임, 그림자, 착수 물결을 그린다.
// 물결 위치는 SkipEffectV2가 전역 배열 _SkipRippleData(xy 위치, z 시작 시간, w 세기)로 넘긴다.
Shader "Custom/WaterV2"
{
    Properties
    {
        [Header(Color)]
        _DeepColor ("Deep Color", Color) = (0.02, 0.11, 0.2, 1)
        _ShallowColor ("Shallow Color", Color) = (0.07, 0.38, 0.48, 1)
        _FoamColor ("Foam Color", Color) = (0.95, 0.98, 1, 1)

        [Header(Waves)]
        _WaveStrength ("Wave Strength", Range(0, 2)) = 0.7
        _WaveScale ("Wave Scale", Range(0.2, 5)) = 1
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 1

        [Header(Lighting)]
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.85
        _Roughness ("Reflection Roughness", Range(0, 1)) = 0.08
        _SpecularStrength ("Sun Specular", Range(0, 10)) = 3
        _SpecularPower ("Sun Specular Sharpness", Range(16, 2048)) = 900
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.6

        [Header(Ripples)]
        _RippleSpeed ("Ripple Speed", Float) = 7
        _RippleWavelength ("Ripple Wavelength", Float) = 1.6
        _RippleLifetime ("Ripple Lifetime", Float) = 1.8
        _RippleNormalStrength ("Ripple Normal Strength", Range(0, 2)) = 0.8
        _RippleFoam ("Ripple Foam", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _DeepColor;
            half4 _ShallowColor;
            half4 _FoamColor;
            float _WaveStrength;
            float _WaveScale;
            float _WaveSpeed;
            half _ReflectionStrength;
            half _Roughness;
            half _SpecularStrength;
            half _SpecularPower;
            half _ShadowStrength;
            float _RippleSpeed;
            float _RippleWavelength;
            float _RippleLifetime;
            half _RippleNormalStrength;
            half _RippleFoam;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define WAVE_COUNT 8
            #define RIPPLE_COUNT 16

            // xy: 진행 방향, z: 파장(m), w: 가파름(기울기 최대값)
            static const float4 WAVES[WAVE_COUNT] =
            {
                float4(0.940, 0.342, 31.0, 0.10),
                float4(0.819, -0.574, 17.0, 0.10),
                float4(0.259, 0.966, 9.3, 0.09),
                float4(0.342, -0.940, 5.7, 0.08),
                float4(0.985, 0.174, 3.1, 0.07),
                float4(-0.643, 0.766, 1.9, 0.06),
                float4(-0.866, -0.500, 1.1, 0.05),
                float4(0.643, 0.766, 0.7, 0.04)
            };

            float4 _SkipRippleData[RIPPLE_COUNT];

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            // 깊은 물 분산 관계(속도 = sqrt(g/k))를 따르는 파도들의 수면 기울기 합.
            // 한 픽셀보다 짧은 파도는 깜빡이므로 footprint에 따라 지운다.
            float2 WaveSlope(float2 xz, float time, float footprint)
            {
                float2 slope = 0;
                [unroll]
                for (int i = 0; i < WAVE_COUNT; i++)
                {
                    float4 wave = WAVES[i];
                    float wavelength = wave.z * _WaveScale;
                    float k = TWO_PI / wavelength;
                    float omega = sqrt(9.81 * k) * _WaveSpeed;
                    float phase = k * dot(wave.xy, xz) - omega * time;
                    float fade = saturate(wavelength / max(footprint, 1e-4) * 0.25 - 0.5);
                    slope += wave.w * cos(phase) * wave.xy * fade;
                }
                return slope * _WaveStrength;
            }

            // 착수 지점에서 바깥으로 퍼지는 고리 물결과 거품.
            void AddRipples(float2 xz, float time, inout float2 slope, inout float foam)
            {
                float k = TWO_PI / max(_RippleWavelength, 0.01);
                float sigma2 = _RippleWavelength * _RippleWavelength * 2.0;
                [unroll]
                for (int i = 0; i < RIPPLE_COUNT; i++)
                {
                    float4 ripple = _SkipRippleData[i];
                    float age = time - ripple.z;
                    float life = 1.0 - age / max(_RippleLifetime, 0.01);
                    if (age < 0.0 || life <= 0.0)
                    {
                        continue;
                    }

                    float2 offset = xz - ripple.xy;
                    float dist = length(offset);
                    float2 dir = offset / max(dist, 1e-3);
                    float x = dist - age * _RippleSpeed;
                    float envelope = exp(-x * x / sigma2);
                    float amplitude = ripple.w * life * life;

                    slope += dir * (cos(k * x) * envelope * amplitude * _RippleNormalStrength);
                    foam += envelope * amplitude * (0.5 + 0.5 * sin(k * x)) * _RippleFoam;
                    // 착수 직후 가운데에 잠깐 남는 거품.
                    foam += saturate(1.0 - dist / (1.0 + age * 2.0)) * amplitude * saturate(1.0 - age / 0.6) * _RippleFoam;
                }
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float time = _Time.y;
                float footprint = max(length(ddx(positionWS.xz)), length(ddy(positionWS.xz)));

                float2 slope = WaveSlope(positionWS.xz, time, footprint);
                float foam = 0;
                AddRipples(positionWS.xz, time, slope, foam);
                float3 normalWS = normalize(float3(-slope.x, 1.0, -slope.y));

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                half shadow = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);

                half NdotV = saturate(dot(normalWS, viewDirWS));
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half fresnel = 0.02 + 0.98 * pow(1.0 - NdotV, 5.0);

                // 파도가 가팔라도 물속 방향을 비추지 않게 반사 방향을 위로 접는다.
                half3 reflectDir = reflect(-viewDirWS, normalWS);
                reflectDir.y = abs(reflectDir.y);
                half3 reflection = GlossyEnvironmentReflection(reflectDir, positionWS, _Roughness, 1.0);

                // 내려다볼수록 깊은 색, 비스듬히 볼수록 얕은 색.
                half3 ambient = SampleSH(normalWS);
                half3 body = lerp(_ShallowColor.rgb, _DeepColor.rgb, NdotV);
                half3 bodyLit = body * (mainLight.color * (0.5 + 0.5 * NdotL) * shadow + ambient * 0.5);

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                half specular = pow(saturate(dot(normalWS, halfDir)), _SpecularPower) * _SpecularStrength * shadow;

                half3 color = lerp(bodyLit, reflection, fresnel * _ReflectionStrength);
                color += specular * mainLight.color;
                color = lerp(color, _FoamColor.rgb * (ambient + mainLight.color * NdotL * shadow), saturate(foam));
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
            #pragma vertex DepthVert
            #pragma fragment DepthNormalsFrag

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            // SSAO가 쓰는 노멀. 물결 디테일 없이 위쪽 방향만 쓴다.
            half4 DepthNormalsFrag() : SV_Target
            {
                return half4(0.0, 1.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
}
