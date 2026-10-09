Shader "SG_BlackHole"
{
    Properties
    {
        _ScreenSpaceScale("Screen Space Scale", Float) = 2
        Vector1_0bb6c794ceb5476bbe1601bd36eb7262("_DistortionExponent", Range(1, 16)) = 4
        _SpherePercentage("_SpherePercentage", Range(0, 1)) = 0.25
        Vector1_e1206881e1244ef4a008548fa38caa6a("_OuterGlowMultiplier", Float) = 1
        Vector1_486f1fcc37a949ffb726a117eab0987a("_OuterGlowExponent", Float) = 4
        Color_f649559c5a534a89a4820a4d0c46d676("_OuterGlowTint", Color) = (1, 1, 1, 0)
        _LensStrength("Gravity Lens Strength", Range(0, 2)) = 0.65
        _EdgeFadeStart("Edge Fade Start", Range(0.2, 0.99)) = 0.75
        _DepthThickness("Scene Depth Thickness", Range(0.001, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
            "UniversalMaterialType" = "Unlit"
        }

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            Blend One OneMinusSrcAlpha
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "AccretionDisk.hlsl"

            TEXTURE2D_X(_BlackHoleBackgroundTexture);
            float4 _BlackHoleBackgroundTexture_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _ScreenSpaceScale;
                float Vector1_0bb6c794ceb5476bbe1601bd36eb7262;
                float _SpherePercentage;
                float Vector1_e1206881e1244ef4a008548fa38caa6a;
                float Vector1_486f1fcc37a949ffb726a117eab0987a;
                float4 Color_f649559c5a534a89a4820a4d0c46d676;
                float _LensStrength;
                float _EdgeFadeStart;
                float _DepthThickness;
                float4 _LensBackgroundColor;
                float _DiskEnabled;
                float4x4 _DiskWorldToLocal;
                float4 _DiskInnerColor;
                float4 _DiskOuterColor;
                float _DiskIntensity;
                float _DiskOpacity;
                float _DiskInnerRadius;
                float _DiskOuterRadius;
                float _DiskEdgeSoftness;
                float _DiskRotationSpeed;
                float _DiskNoiseScale;
                float _DiskSpiralStrength;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float4 screenPos  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = nrm.normalWS;
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(pos.positionWS);
                OUT.screenPos = ComputeScreenPos(pos.positionCS);

                return OUT;
            }

            // Exact equivalent of the supplied SF_Raycast.hlsl.
            // IMPORTANT: RayDirection is the Shader Graph View Direction,
            // i.e. from the surface toward the camera. The original function
            // deliberately uses -RayDirection as the forward ray direction.
            void Raycast_float(
                float3 RayOrigin,
                float3 RayDirection,
                float3 SphereOrigin,
                float SphereSize,
                out float Hit,
                out float3 HitPosition,
                out float3 HitNormal)
            {
                HitPosition = float3(0.0, 0.0, 0.0);
                HitNormal = float3(0.0, 0.0, 0.0);

                float t = 0.0f;
                float3 L = SphereOrigin - RayOrigin;
                float tca = dot(L, -RayDirection);

                if (tca < 0.0)
                {
                    Hit = 0.0;
                    return;
                }

                float d2 = dot(L, L) - tca * tca;
                float radius2 = SphereSize * SphereSize;

                if (d2 > radius2)
                {
                    Hit = 0.0;
                    return;
                }

                float thc = sqrt(radius2 - d2);
                t = tca - thc;

                Hit = 1.0;
                HitPosition = RayOrigin - RayDirection * t;
                HitNormal = normalize(HitPosition - SphereOrigin);
            }

            // Exact equivalent of GetScreenPosition_float() in SF_Raycast.hlsl.
            void GetScreenPosition(
                float3 Position,
                out float2 ScreenPosition,
                out float2 ScreenPositionAspectRatio)
            {
                // Unity 6 / current URP exposes ComputeScreenPos(float4) only.
                // The URP implementation itself applies _ProjectionParams.x.
                float4 screen = ComputeScreenPos(
                    TransformWorldToHClip(Position)
                );

                ScreenPosition = screen.xy / abs(screen.w);

                float aspectRatio = _ScreenParams.y / _ScreenParams.x;
                ScreenPositionAspectRatio =
                    float2(ScreenPosition.x, ScreenPosition.y * aspectRatio);
            }

            // Exact equivalent of MirrorUVCoordinates_float().
            float2 MirrorUVCoordinates(float2 UVs)
            {
                float2 NewUVs;

                if (UVs.x < 0.0 || UVs.x > 1.0)
                    NewUVs.x =
                        1.0 - abs((UVs.x - 2.0 * floor(UVs.x / 2.0)) - 1.0);
                else
                    NewUVs.x = UVs.x;

                if (UVs.y < 0.0 || UVs.y > 1.0)
                    NewUVs.y =
                        1.0 - abs((UVs.y - 2.0 * floor(UVs.y / 2.0)) - 1.0);
                else
                    NewUVs.y = UVs.y;

                return NewUVs;
            }

            float3 ObjectScaleWS()
            {
                return float3(
                    length(float3(UNITY_MATRIX_M[0].x,
                                  UNITY_MATRIX_M[1].x,
                                  UNITY_MATRIX_M[2].x)),
                    length(float3(UNITY_MATRIX_M[0].y,
                                  UNITY_MATRIX_M[1].y,
                                  UNITY_MATRIX_M[2].y)),
                    length(float3(UNITY_MATRIX_M[0].z,
                                  UNITY_MATRIX_M[1].z,
                                  UNITY_MATRIX_M[2].z))
                );
            }

            /// <summary>
            /// 중심 기준 광선과 반지름을 사용하여 구체 진입 및 이탈 거리를 반환한다.
            /// </summary>
            bool SphereInterval(float3 origin, float3 direction, float radius, out float2 interval)
            {
                float closest = -dot(origin, direction);
                float discriminant = closest * closest - dot(origin, origin) + radius * radius;
                float halfLength = sqrt(max(discriminant, 0.0));
                interval = float2(closest - halfLength, closest + halfLength);
                return discriminant >= 0.0 && interval.y > 0.0;
            }

            /// <summary>
            /// 중심 기준 위치와 광선 방향을 사용하여 외곽에서 감쇠하는 수직 중력 가속도를 반환한다.
            /// </summary>
            float3 BendRay(float3 position, float3 direction, float proxyRadius, float coreRadius, float lensWeight)
            {
                float radiusSquared = dot(position, position);
                float radius = sqrt(radiusSquared);
                float fieldFade = 1.0 - smoothstep(proxyRadius * 0.8, proxyRadius, radius);
                float3 transverse = position - direction * dot(position, direction);
                return -_LensStrength * _ScreenSpaceScale * lensWeight * coreRadius * fieldFade * transverse
                    / max(radiusSquared * radius, max(coreRadius * coreRadius * coreRadius, 0.0001));
            }

            /// <summary>
            /// 월드 좌표 선분과 원반 설정을 사용하여 중심 앞의 교차점 발광과 투과율을 누적한다.
            /// </summary>
            void AccumulateDisk(float3 startWS, float3 endWS, float maximumFraction,
                AccretionDiskSettings settings, inout float3 color, inout float transmittance)
            {
                if (_DiskEnabled < 0.5)
                    return;
                float3 startLocal = mul(_DiskWorldToLocal, float4(startWS, 1.0)).xyz;
                float3 endLocal = mul(_DiskWorldToLocal, float4(endWS, 1.0)).xyz;
                float denominator = startLocal.z - endLocal.z;
                if (abs(denominator) < 0.000001)
                    return;
                float fraction = startLocal.z / denominator;
                if (fraction <= 0.00001 || fraction > maximumFraction)
                    return;
                float2 diskPosition = lerp(startLocal.xy, endLocal.xy, fraction);
                float4 disk = EvaluateAccretionDisk(diskPosition, settings);
                color += transmittance * disk.rgb;
                transmittance *= 1.0 - disk.a;
            }

            /// <summary>
            /// 화면 UV를 사용해 광선 반복문에서도 미분 없이 불투명 색상을 샘플링하여 반환한다.
            /// </summary>
            float3 SampleOpaqueColor(float2 uv)
            {
                uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraOpaqueTexture_TexelSize.xy);
                return SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv, 0).rgb;
            }

            /// <summary>
            /// 화면 UV에서 점 샘플러와 고정 밉 레벨을 사용해 미분 없는 씬 깊이를 반환한다.
            /// </summary>
            float SampleOpaqueDepth(float2 uv)
            {
                uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraDepthTexture_TexelSize.xy);
                return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0).r;
            }

            /// <summary>
            /// 화면 UV에서 원반을 제외하고 캡처한 배경 색상을 반환하여 최종 화면의 중복 합성을 방지한다.
            /// </summary>
            float3 SampleCapturedBackground(float2 uv)
            {
                uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _BlackHoleBackgroundTexture_TexelSize.xy);
                return SAMPLE_TEXTURE2D_X_LOD(_BlackHoleBackgroundTexture, sampler_CameraOpaqueTexture, uv, 0).rgb;
            }

            /// <summary>
            /// 색상 텍스처의 보간 범위까지 깊이를 확인해 오브젝트 색이 섞이지 않은 배경 여부를 반환한다.
            /// </summary>
            bool IsCapturedSky(float2 uv)
            {
                float farDepth = _ProjectionParams.z * 0.999;
                float2 padding = _CameraOpaqueTexture_TexelSize.xy * 2.0;
                return LinearEyeDepth(SampleOpaqueDepth(uv), _ZBufferParams) >= farDepth
                    && LinearEyeDepth(SampleOpaqueDepth(uv + float2(padding.x, 0.0)), _ZBufferParams) >= farDepth
                    && LinearEyeDepth(SampleOpaqueDepth(uv - float2(padding.x, 0.0)), _ZBufferParams) >= farDepth
                    && LinearEyeDepth(SampleOpaqueDepth(uv + float2(0.0, padding.y)), _ZBufferParams) >= farDepth
                    && LinearEyeDepth(SampleOpaqueDepth(uv - float2(0.0, padding.y)), _ZBufferParams) >= farDepth;
            }

            /// <summary>
            /// 교차 없는 광선의 화면 UV에서 배경만 찾고, 가려진 배경이 없으면 카메라 배경색을 반환한다.
            /// 깊이가 있는 오브젝트를 교차 판정 없이 복제하지 않는다.
            /// </summary>
            float3 SampleLensBackground(float2 uv)
            {
                float3 result = _LensBackgroundColor.rgb;
                bool found = IsCapturedSky(uv);
                if (found)
                    result = SampleCapturedBackground(uv);
                [unroll]
                for (int ring = 0; ring < 4; ring++)
                {
                    if (found)
                        break;
                    float radius = 0.015 * exp2(ring);
                    [unroll]
                    for (int side = 0; side < 4; side++)
                    {
                        float2 offset = side == 0 ? float2(radius, 0.0)
                            : side == 1 ? float2(-radius, 0.0)
                            : side == 2 ? float2(0.0, radius) : float2(0.0, -radius);
                        float2 candidate = uv + offset;
                        if (any(candidate < 0.0) || any(candidate > 1.0))
                            continue;
                        if (IsCapturedSky(candidate))
                        {
                            result = SampleCapturedBackground(candidate);
                            found = true;
                            break;
                        }
                    }
                }
                return result;
            }

            /// <summary>
            /// 월드 위치를 화면에 투영하여 광선과 첫 불투명 표면의 선형 시선 깊이 차이를 반환한다.
            /// 화면 밖과 배경에는 유효 표면이 없음을 반환한다.
            /// </summary>
            bool SceneDepthGap(float3 positionWS, out float gap, out float sceneDepth, out float2 uv)
            {
                float4 screen = ComputeScreenPos(TransformWorldToHClip(positionWS));
                uv = screen.xy / max(screen.w, 0.0001);
                gap = -_ProjectionParams.z;
                sceneDepth = _ProjectionParams.z;
                if (screen.w <= 0.0 || any(uv < 0.0) || any(uv > 1.0))
                    return false;
                sceneDepth = LinearEyeDepth(SampleOpaqueDepth(uv), _ZBufferParams);
                if (sceneDepth >= _ProjectionParams.z * 0.999)
                    return false;
                gap = -TransformWorldToView(positionWS).z - sceneDepth;
                return true;
            }

            /// <summary>
            /// 광선 선분이 씬 표면의 앞에서 뒤로 통과하는 지점을 정밀화해 교차 비율과 색상 UV를 반환한다.
            /// 불연속 깊이와 허용 두께 밖의 교차는 제외한다.
            /// </summary>
            bool IntersectScene(float3 startWS, float3 endWS, out float fraction, out float2 hitUV)
            {
                fraction = 2.0;
                hitUV = 0.0;
                if (TransformWorldToView(endWS).z >= TransformWorldToView(startWS).z)
                    return false;
                float startGap = 0.0, endGap = 0.0, startDepth = 0.0, endDepth = 0.0;
                float2 startUV = 0.0, endUV = 0.0;
                SceneDepthGap(startWS, startGap, startDepth, startUV);
                bool endValid = SceneDepthGap(endWS, endGap, endDepth, endUV);
                if (!endValid || startGap >= 0.0 || endGap < 0.0)
                    return false;

                float low = 0.0;
                float high = 1.0;
                [unroll]
                for (int refinement = 0; refinement < 5; refinement++)
                {
                    float middle = (low + high) * 0.5;
                    float middleGap = 0.0, middleDepth = 0.0;
                    float2 middleUV = 0.0;
                    bool valid = SceneDepthGap(lerp(startWS, endWS, middle), middleGap, middleDepth, middleUV);
                    if (valid && middleGap >= 0.0)
                        high = middle;
                    else
                        low = middle;
                }

                float lowGap = 0.0, highGap = 0.0, lowDepth = 0.0, highDepth = 0.0;
                float2 lowUV = 0.0;
                float3 lowPosition = lerp(startWS, endWS, low);
                float3 highPosition = lerp(startWS, endWS, high);
                bool lowValid = SceneDepthGap(lowPosition, lowGap, lowDepth, lowUV);
                bool highValid = SceneDepthGap(highPosition, highGap, highDepth, hitUV);
                float thickness = _DepthThickness + abs(TransformWorldToView(highPosition).z - TransformWorldToView(lowPosition).z);
                if (!lowValid || !highValid || highGap < 0.0 || highGap > thickness || abs(highDepth - lowDepth) > thickness * 2.0)
                    return false;
                fraction = high;
                return true;
            }

            /// <summary>
            /// 선분의 씬 표면 및 코어 종료 거리와 원반을 앞뒤 순서로 합성하고 광선 종료 여부를 반환한다.
            /// 가까운 불투명 표면보다 뒤의 원반은 누적하지 않는다.
            /// </summary>
            bool ResolveSegment(float3 startWS, float3 endWS, float coreDistance, float3 coreGlow,
                AccretionDiskSettings settings, inout float3 color, inout float transmittance)
            {
                float sceneFraction = 2.0;
                float2 sceneUV = 0.0;
                bool sceneHit = IntersectScene(startWS, endWS, sceneFraction, sceneUV);
                float segmentLength = distance(startWS, endWS);
                float coreFraction = coreDistance >= 0.0 && coreDistance <= segmentLength
                    ? coreDistance / max(segmentLength, 0.0001) : 2.0;
                float terminalFraction = min(sceneFraction, coreFraction);
                AccumulateDisk(startWS, endWS, min(1.0, terminalFraction - 0.00001), settings, color, transmittance);
                bool resolved = false;
                if (sceneHit && sceneFraction + 0.00001 < coreFraction)
                {
                    color += transmittance * SampleOpaqueColor(sceneUV);
                    resolved = true;
                }
                else if (coreFraction <= 1.0)
                {
                    color += transmittance * coreGlow;
                    resolved = true;
                }
                return resolved;
            }

            /// <summary>
            /// 화면 광선을 중력장으로 추적해 형제 원반과 배경을 합성하고 외곽 감쇠 알파를 반환한다.
            /// </summary>
            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 rayDirection = normalize(IN.positionWS - _WorldSpaceCameraPos);
                float3 initialDirectionVS = mul((float3x3)UNITY_MATRIX_V, rayDirection);
                float3 rayOrigin = _WorldSpaceCameraPos - centerWS;
                float scale = ObjectScaleWS().x;
                float proxyRadius = scale * 0.5;
                float coreRadius = min(_SpherePercentage * scale, proxyRadius * 0.95);
                float2 interval;
                if (!SphereInterval(rayOrigin, rayDirection, proxyRadius, interval))
                    return 0;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float entryDistance = max(interval.x, 0.0);
                float3 position = rayOrigin + rayDirection * entryDistance;
                float originalSceneDepth = LinearEyeDepth(SampleOpaqueDepth(screenUV), _ZBufferParams);
                float entryDepth = -TransformWorldToView(position + centerWS).z;
                if (originalSceneDepth < entryDepth - 0.001)
                    return 0;

                float closest = -dot(rayOrigin, rayDirection);
                float normalizedImpact = length(rayOrigin + rayDirection * closest) / proxyRadius;
                float ownership = pow(saturate(1.0 - smoothstep(_EdgeFadeStart, 1.0, normalizedImpact)),
                    Vector1_0bb6c794ceb5476bbe1601bd36eb7262);

                AccretionDiskSettings settings;
                settings.innerColor = _DiskInnerColor.rgb;
                settings.outerColor = _DiskOuterColor.rgb;
                settings.intensity = _DiskIntensity;
                settings.opacity = _DiskOpacity;
                settings.innerRadius = _DiskInnerRadius;
                settings.outerRadius = _DiskOuterRadius;
                settings.edgeSoftness = _DiskEdgeSoftness;
                settings.rotationSpeed = _DiskRotationSpeed;
                settings.noiseScale = _DiskNoiseScale;
                settings.spiralStrength = _DiskSpiralStrength;

                float3 diskColor = 0.0;
                float transmittance = 1.0;

                // 검은 원의 화면 크기는 유지하되, 코어에 도달하기 전의 씬 표면과 원반을 먼저 판정한다.
                float2 coreInterval;
                float coreDistance = 1e20;
                float3 coreGlow = 0.0;
                if (coreRadius > 0.0 && SphereInterval(rayOrigin, rayDirection, coreRadius, coreInterval))
                {
                    coreDistance = max(coreInterval.x - entryDistance, 0.0);
                    float3 corePosition = rayOrigin + rayDirection * max(coreInterval.x, 0.0);
                    float fresnel = pow(saturate(1.0 - dot(normalize(corePosition), -rayDirection)),
                        Vector1_486f1fcc37a949ffb726a117eab0987a);
                    coreGlow = Color_f649559c5a534a89a4820a4d0c46d676.rgb
                        * Vector1_e1206881e1244ef4a008548fa38caa6a * fresnel;
                }

                AccumulateDisk(_WorldSpaceCameraPos, position + centerWS, 1.0, settings, diskColor, transmittance);
                float stepLength = proxyRadius * (2.0 / 64.0);
                float traveled = 0.0;

                [loop]
                for (int step = 0; step < 96; step++)
                {
                    float3 acceleration = BendRay(position, rayDirection, proxyRadius, coreRadius, ownership);
                    float3 middleDirection = normalize(rayDirection + acceleration * stepLength * 0.5);
                    float3 middlePosition = position + middleDirection * stepLength * 0.5;
                    float3 nextPosition = position + middleDirection * stepLength;
                    float3 nextDirection = normalize(rayDirection
                        + BendRay(middlePosition, middleDirection, proxyRadius, coreRadius, ownership) * stepLength);

                    if (ResolveSegment(position + centerWS, nextPosition + centerWS, coreDistance - traveled,
                        coreGlow, settings, diskColor, transmittance))
                        return half4(diskColor, 1.0);
                    traveled += stepLength;
                    position = nextPosition;
                    rayDirection = nextDirection;
                    if (dot(position, position) >= proxyRadius * proxyRadius && dot(position, rayDirection) > 0.0)
                        break;
                }

                float outsideStep = proxyRadius * 0.12;
                [loop]
                for (int outsideIndex = 0; outsideIndex < 48; outsideIndex++)
                {
                    float3 nextPosition = position + rayDirection * outsideStep;
                    if (ResolveSegment(position + centerWS, nextPosition + centerWS, coreDistance - traveled,
                        coreGlow, settings, diskColor, transmittance))
                        return half4(diskColor, 1.0);
                    traveled += outsideStep;
                    position = nextPosition;
                    float4 projected = TransformWorldToHClip(position + centerWS);
                    if (projected.w <= 0.0 || -TransformWorldToView(position + centerWS).z >= _ProjectionParams.z)
                        break;
                    outsideStep *= 1.15;
                }

                // 큰 굴절각에서도 화면 투영의 분모가 0에 가까워지지 않도록 방향의 각도 차이를 사용한다.
                float3 outgoingDirectionVS = mul((float3x3)UNITY_MATRIX_V, rayDirection);
                float2 angularOffset = atan2(outgoingDirectionVS.xy, -outgoingDirectionVS.z)
                    - atan2(initialDirectionVS.xy, -initialDirectionVS.z);
                float2 projectionScale = float2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1] * _ProjectionParams.x) * 0.5;
                float2 outgoingUV = screenUV + angularOffset * projectionScale;
                float2 distortedUV = MirrorUVCoordinates(lerp(screenUV, outgoingUV, _ScreenSpaceScale * ownership));
                float3 background = SampleLensBackground(distortedUV);
                return half4(diskColor + transmittance * background, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }

            Cull Back
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.normalWS = nrm.normalWS;

                return OUT;
            }

            half4 DepthNormalsFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS = normalize(IN.normalWS);
                return half4(normalWS * 0.5h + 0.5h, 0.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Back
            ZTest LEqual
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // URP supplies these globals to the ShadowCaster pass.
            // They must be declared explicitly when the pass is written inline.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float3 biasedPositionWS =
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS);

                OUT.positionCS = TransformWorldToHClip(biasedPositionWS);

                #if UNITY_REVERSED_Z
                    OUT.positionCS.z =
                        min(OUT.positionCS.z, OUT.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z =
                        max(OUT.positionCS.z, OUT.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
