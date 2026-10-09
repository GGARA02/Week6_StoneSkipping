using UnityEngine;

// 게의 특수 동작. SPACE 판정에 성공하면 입에서 게거품 파티클을 순간적으로 뿜어낸다.
public class CrabAbility : FishAbility
{
    [Header("이펙트")]
    [Tooltip("SPACE 판정 성공 시 재생할 게거품 파티클 시스템")]
    [SerializeField]
    private ParticleSystem _crabBubble;

    /// <summary>
    /// 던진 상태에서 SPACE 판정에 성공했을 때 게거품 파티클을 재생한다.
    /// _crabBubble을 사용하며, 파티클 재생 상태를 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        _crabBubble.Play();
    }
}
