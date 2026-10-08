using System;

using UnityEngine;

public enum FishShape
{
    Fish,
    Crab,
    Tire,
    Can,
}

// 물고기 한 종류의 값, 생김새, 돌 대신 던질 때의 물리 특성.
// 물고기는 옆모습 실루엣 기준으로 길이는 z, 높이는 x, 두께는 y로 만든다. 게는 위에서 본 모양이라 높이가 좌우 폭이다.
[Serializable]
public class FishType
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

    [Header("선택 미리보기")]
    [Tooltip("공개하면 회색 반투명 모델, 공개하지 않으면 물음표로 표시한다. 도감 등록 여부와는 별개다")]
    [SerializeField]
    private bool _isRevealed = true;

    [Header("몸")]
    [SerializeField]
    private FishShape _shape = FishShape.Fish;
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
    public bool IsRevealed => _isRevealed;
    public FishShape Shape => _shape;
    public float Length => _length;
    public float Height => _height;
    public float Thickness => _thickness;
    public float TailLength => _tailLength;
    public float DorsalFin => _dorsalFin;

    /// <summary>
    /// 이 물고기를 던질 때 적용할 배율을 만든다. 던지는 힘과 스핀 업그레이드 배율을 곱한다.
    /// powerMultiplier, spinMultiplier를 사용하며, ThrowModifiers를 반환한다.
    /// </summary>
    public ThrowModifiers ToThrowModifiers(float powerMultiplier, float spinMultiplier)
    {
        return new ThrowModifiers(
            _speedMultiplier * powerMultiplier,
            _spinMultiplier * spinMultiplier,
            _liftMultiplier,
            _frictionMultiplier,
            _airGravityScale);
    }
}
