using UnityEngine;

// Week4 화살의 특수 동작. 던질 거리로 고른 동안에는 위아래로 천천히 흔들리고,
// 던진 뒤 SPACE 판정에 성공하면 불씨 파티클이 터져 나왔다가 화살로 빨려 들어간다.
public class Week4ArrowAbility : FishAbility
{
    [Header("고르기")]
    [Tooltip("위아래로 흔들리는 높이")]
    [SerializeField]
    private float _bobHeight = 0.2f;
    [Tooltip("초당 흔들리는 횟수")]
    [SerializeField]
    private float _bobFrequency = 0.5f;

    [Header("불씨")]
    [Tooltip("판정 성공 때 화살 위치에 만드는 불씨 파티클")]
    [SerializeField]
    private Week4ArrowGainEffect _gainEffectPrefab;

    [Header("발사")]
    [Tooltip("던진 뒤 남는 시간을 바꿀 트레일")]
    [SerializeField]
    private TrailRenderer _trail;
    [Tooltip("던진 뒤 트레일이 남는 시간(초)")]
    [SerializeField]
    private float _thrownTrailTime = 1f;

    [Header("미끄러짐")]
    [Tooltip("미끄러지는 동안 끄는 파티클")]
    [SerializeField]
    private ParticleSystem _particle;

    [Header("상태")]
    private bool _held;
    private bool _thrown;
    private bool _sliding;

    void Start()
    {
        // 던질 거리로 만들어지면 모양 생성기 아래에 붙는다. 물에서 튀어 오르는 화살은 흔들지 않는다.
        _held = GetComponentInParent<FishMeshGenerator>() != null;
    }

    void Update()
    {
        if (!_held || _thrown) return;

        float offset = Mathf.Sin(Time.time * _bobFrequency * Mathf.PI * 2f) * _bobHeight;
        // 조준 자세로 물고기가 기울어도 화면 기준 위아래로 흔들리게 월드 위쪽을 부모 로컬로 바꾼다.
        transform.localPosition = transform.parent.InverseTransformVector(Vector3.up * offset);
    }

    /// <summary>
    /// 던진 뒤 처음 불리면 흔들림을 멈추고 화살을 제자리로 돌린 뒤 트레일 시간을 던진 뒤 값으로 바꾼다.
    /// _thrownTrailTime을 사용하며, context와 dt는 쓰지 않는다. _thrown, 로컬 위치, 트레일 시간을 변경한다.
    /// </summary>
    public override void UpdateFlight(ThrowContext context, float dt)
    {
        if (_thrown) return;

        _thrown = true;
        transform.localPosition = Vector3.zero;
        _trail.time = _thrownTrailTime;
    }

    /// <summary>
    /// 미끄러지기 시작하면 파티클을 끄고, 이후 판정 성공에서 불씨 파티클을 만들지 않게 한다.
    /// _particle을 사용하며, context는 쓰지 않는다. _sliding과 파티클 재생 상태를 변경한다.
    /// </summary>
    public override void OnSlideStart(ThrowContext context)
    {
        _sliding = true;
        // 이미 나온 파티클은 자연스럽게 사라지도록 새로 만드는 것만 멈춘다.
        _particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>
    /// SPACE 판정에 성공하면 화살 위치에 불씨 파티클을 만들고 화살로 빨려 들어오게 한다. 미끄러지는 중에는 만들지 않는다.
    /// _gainEffectPrefab, _sliding, 현재 위치를 사용하며, context와 judge는 쓰지 않는다. 새 불씨 파티클을 만든다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (_sliding) return;

        Week4ArrowGainEffect effect = Instantiate(_gainEffectPrefab, transform.position, Quaternion.identity);
        effect.SetTarget(transform);
    }
}
