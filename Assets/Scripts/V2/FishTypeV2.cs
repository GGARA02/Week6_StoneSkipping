using System;

using UnityEngine;

public enum FishShapeV2
{
    Fish,
    Crab,
    Tire,
    Can,
}

// 물고기 한 종류의 값, 생김새, 돌 대신 던질 때의 물리 특성.
// 물고기는 옆모습 실루엣 기준으로 길이는 z, 높이는 x, 두께는 y로 만든다. 게는 위에서 본 모양이라 높이가 좌우 폭이다.
[Serializable]
public class FishTypeV2
{
    [Header("기본")]
    [SerializeField]
    private string _id = "mackerel";
    [SerializeField]
    private string _displayName = "MACKEREL";
    [SerializeField]
    private int _value = 10;
    [SerializeField]
    private Color _color = new Color(0.35f, 0.5f, 0.62f);
    [Tooltip("출현 확률 가중치")]
    [SerializeField]
    private float _spawnWeight = 1f;

    [Header("몸")]
    [SerializeField]
    private FishShapeV2 _shape = FishShapeV2.Fish;
    [SerializeField]
    private float _length = 1.6f;
    [Tooltip("물고기는 몸 높이, 게는 좌우 폭")]
    [SerializeField]
    private float _height = 0.45f;
    [SerializeField]
    private float _thickness = 0.3f;
    [Tooltip("물고기는 꼬리 길이, 게는 집게 길이")]
    [SerializeField]
    private float _tailLength = 0.45f;
    [Tooltip("등지느러미 크기. 0이면 없다 (상어용)")]
    [SerializeField]
    private float _dorsalFin = 0f;
    [Tooltip("닿으면 부푸는 배율. 0이면 부풀지 않는다 (복어용)")]
    [SerializeField]
    private float _inflateScale = 0f;

    [Header("돌 대신 던질 때")]
    [SerializeField]
    private float _speedMultiplier = 1f;
    [SerializeField]
    private float _spinMultiplier = 1f;
    [Tooltip("물고기는 돌과 면적이 달라서 양력을 보정한다")]
    [SerializeField]
    private float _liftMultiplier = 3f;
    [SerializeField]
    private float _frictionMultiplier = 1f;
    [Tooltip("공중에서 받는 중력 배율. 1보다 작으면 활공한다")]
    [SerializeField]
    private float _airGravityScale = 1f;

    public string Id => _id;
    public string DisplayName => _displayName;
    public int Value => _value;
    public Color Color => _color;
    public float SpawnWeight => _spawnWeight;
    public FishShapeV2 Shape => _shape;
    public float Length => _length;
    public float Height => _height;
    public float Thickness => _thickness;
    public float TailLength => _tailLength;
    public float DorsalFin => _dorsalFin;
    public float InflateScale => _inflateScale;

    /// <summary>
    /// 코드에서 기본 물고기 종류를 만들 때 쓰는 생성자.
    /// 각 인자를 그대로 필드에 저장한다.
    /// </summary>
    public FishTypeV2(string id, string displayName, int value, Color color, float spawnWeight,
        float length, float height, float thickness, float tailLength,
        float speedMultiplier, float spinMultiplier, float liftMultiplier, float frictionMultiplier, float airGravityScale,
        FishShapeV2 shape = FishShapeV2.Fish, float dorsalFin = 0f, float inflateScale = 0f)
    {
        _id = id;
        _displayName = displayName;
        _value = value;
        _color = color;
        _spawnWeight = spawnWeight;
        _length = length;
        _height = height;
        _thickness = thickness;
        _tailLength = tailLength;
        _speedMultiplier = speedMultiplier;
        _spinMultiplier = spinMultiplier;
        _liftMultiplier = liftMultiplier;
        _frictionMultiplier = frictionMultiplier;
        _airGravityScale = airGravityScale;
        _shape = shape;
        _dorsalFin = dorsalFin;
        _inflateScale = inflateScale;
    }

    /// <summary>
    /// 이 물고기를 던질 때 적용할 배율을 만든다. 던지는 힘과 스핀 업그레이드 배율을 곱한다.
    /// powerMultiplier, spinMultiplier를 사용하며, ThrowModifiersV2를 반환한다.
    /// </summary>
    public ThrowModifiersV2 ToThrowModifiers(float powerMultiplier, float spinMultiplier)
    {
        return new ThrowModifiersV2(
            _speedMultiplier * powerMultiplier,
            _spinMultiplier * spinMultiplier,
            _liftMultiplier,
            _frictionMultiplier,
            _airGravityScale);
    }
}
