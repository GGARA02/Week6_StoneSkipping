using UnityEngine;

public class EndigCreditFish : EjectFishAbility
{
    [SerializeField] private ContributorList _contributor;

    /// <summary>
    /// 던진 상태에서 SPACE 판정에 성공했을 때 이름을 소환한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        //무작위 이름 하나 선택해서 흩뿌리기
    }
}
