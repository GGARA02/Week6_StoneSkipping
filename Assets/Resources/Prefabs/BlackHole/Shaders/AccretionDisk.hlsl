#ifndef BLACK_HOLE_ACCRETION_DISK_INCLUDED
#define BLACK_HOLE_ACCRETION_DISK_INCLUDED

struct AccretionDiskSettings
{
    float3 innerColor;
    float3 outerColor;
    float intensity;
    float opacity;
    float innerRadius;
    float outerRadius;
    float edgeSoftness;
    float rotationSpeed;
    float noiseScale;
    float spiralStrength;
};

/// <summary>
/// 격자 좌표를 사용하여 0부터 1 사이의 의사 난수 값을 반환한다.
/// </summary>
float DiskHash(float2 samplePosition)
{
    samplePosition = frac(samplePosition * float2(123.34, 456.21));
    samplePosition += dot(samplePosition, samplePosition + 45.32);
    return frac(samplePosition.x * samplePosition.y);
}

/// <summary>
/// 샘플 좌표 주변 격자 난수를 보간하여 연속적인 노이즈 값을 반환한다.
/// </summary>
float DiskNoise(float2 samplePosition)
{
    float2 cell = floor(samplePosition);
    float2 weight = frac(samplePosition);
    weight = weight * weight * (3.0 - 2.0 * weight);
    return lerp(
        lerp(DiskHash(cell), DiskHash(cell + float2(1, 0)), weight.x),
        lerp(DiskHash(cell + float2(0, 1)), DiskHash(cell + float2(1, 1)), weight.x),
        weight.y);
}

/// <summary>
/// 원반 로컬 위치와 외형 설정을 사용하여 현재 시간의 알파 선곱 발광 색상을 반환한다.
/// </summary>
float4 EvaluateAccretionDisk(float2 localPosition, AccretionDiskSettings settings)
{
    float radius = length(localPosition);
    float innerRadius = min(settings.innerRadius, settings.outerRadius - 0.001);
    float innerMask = innerRadius > 0.0 ? smoothstep(innerRadius, innerRadius + settings.edgeSoftness, radius) : 1.0;
    float radialMask = innerMask
        * (1.0 - smoothstep(settings.outerRadius - settings.edgeSoftness, settings.outerRadius, radius));
    float radialT = saturate((radius - innerRadius) / max(settings.outerRadius - innerRadius, 0.001));
    float angle = _Time.y * settings.rotationSpeed / (0.35 + radialT) - radius * settings.spiralStrength;
    float sine;
    float cosine;
    sincos(angle, sine, cosine);
    float2 flowingPosition = float2(
        cosine * localPosition.x - sine * localPosition.y,
        sine * localPosition.x + cosine * localPosition.y) * settings.noiseScale;
    float noise = DiskNoise(flowingPosition) * 0.6
        + DiskNoise(flowingPosition * 2.07 + 13.7) * 0.3
        + DiskNoise(flowingPosition * 4.13 - 7.1) * 0.1;
    float bands = 0.5 + 0.5 * sin(radius * 180.0 + noise * 8.0);
    float filaments = lerp(0.3, 1.0, noise) * lerp(0.55, 1.0, bands);
    float alpha = radialMask * settings.opacity;
    float3 color = lerp(settings.innerColor, settings.outerColor, radialT)
        * settings.intensity * filaments * lerp(1.0, 0.35, radialT);
    return float4(color * alpha, alpha);
}

#endif
