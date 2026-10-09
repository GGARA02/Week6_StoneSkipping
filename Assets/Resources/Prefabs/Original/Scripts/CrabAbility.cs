using UnityEngine;

public class CrabAbility : FishAbility
{
    [SerializeField] ParticleSystem _crabBubble;

    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        //게거품 생성

    }
}
