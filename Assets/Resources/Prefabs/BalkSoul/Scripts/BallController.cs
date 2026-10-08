using System.Collections;

using UnityEngine;

public class BallController : FishAbility
{
    [Header("참조")]
    [SerializeField] private Animator _animator;

    [Header("상태")]
    private bool _isAttacking;
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
    /// 던진 상태에서 SPACE 판정에 성공하면 공격 애니메이션을 재생한다.
    /// context와 judge는 쓰지 않으며, 공격 재생 상태를 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        PlayAttackSequence();
    }

    /// <summary>
    /// 수면 타이밍 성공 판정에서 요청한 공격 애니메이션을 순서대로 재생한다.
    /// 입력값은 없으며, 공격 중이 아닐 때만 코루틴을 시작하고 공격 상태를 변경한다.
    /// </summary>
    public void PlayAttackSequence()
    {
        if (_isAttacking) return;

        StartCoroutine(AttackSequence());
    }

    /// <summary>
    /// 공격 1, 2, 3 애니메이션을 각각 1초 간격으로 재생한다.
    /// _animator를 사용하며, 재생 대기 명령을 반환하고 _isAttacking을 변경한다.
    /// </summary>
    private IEnumerator AttackSequence()
    {
        _isAttacking = true;

        _animator.CrossFadeInFixedTime("Attack_Light_1", 0.05f, 0, 0f);
        yield return new WaitForSeconds(1f);

        _animator.CrossFadeInFixedTime("Attack_Light_2", 0.05f, 0, 0f);
        yield return new WaitForSeconds(1f);

        _animator.CrossFadeInFixedTime("Attack_Light_3", 0.05f, 0, 0f);
        yield return new WaitForSeconds(1f);

        _isAttacking = false;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        _isAttacking = false;
    }
}
