using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator _animator;

    [Header("상태")]
    private int _nextAttackIndex = 1;
    private TrailRenderer[] _trails;

    void Awake()
    {
        _trails = GetComponentsInChildren<TrailRenderer>();
        UpdateTrailWidths();
    }

    void LateUpdate()
    {
        UpdateTrailWidths();
    }

    /// <summary>
    /// 현재 오브젝트 크기에 맞춰 공격 궤적의 폭을 갱신한다.
    /// transform.localScale.x를 사용하며, 각 TrailRenderer의 widthMultiplier를 변경한다.
    /// </summary>
    private void UpdateTrailWidths()
    {
        float width = transform.localScale.x * 0.2f;
        foreach (TrailRenderer trail in _trails)
        {
            trail.widthMultiplier = width;
        }
    }

    /// <summary>
    /// 수면 타이밍 성공 판정마다 공격 1, 2, 3 중 다음 애니메이션 하나를 재생한다.
    /// 입력값은 없으며, _animator로 공격을 재생하고 다음 공격 번호를 1부터 3까지 순환한다.
    /// </summary>
    public void PlayAttackSequence()
    {
        _animator.CrossFadeInFixedTime($"Attack_Light_{_nextAttackIndex}", 0.05f, 0, 0f);
        _nextAttackIndex = _nextAttackIndex % 3 + 1;
    }

    void OnDisable()
    {
        _nextAttackIndex = 1;
    }
}
