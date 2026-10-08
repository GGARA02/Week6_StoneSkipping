using UnityEngine;

// 물고기 프리팹 루트에 붙어 종류별 특수 동작을 수행한다. 하위 클래스는 필요한 동작만 재정의한다.
// Fish를 참조하지 않으며, 동작에 필요한 대상은 호출하는 쪽(PlayerController)이 넘겨준다.
public abstract class FishAbility : MonoBehaviour
{
    /// <summary>
    /// 던진 상태에서 SPACE 판정에 성공했을 때 특수 동작을 한다. 기본 동작은 없다.
    /// context와 judge(Perfect 또는 Good)를 사용하며, 변경하는 상태는 하위 클래스가 정한다.
    /// </summary>
    public virtual void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
    }

    /// <summary>
    /// 던진 상태에서 SPACE 판정에 실패(MISS)했을 때 특수 동작을 한다. 기본 동작은 없다.
    /// context를 사용하며, 변경하는 상태는 하위 클래스가 정한다.
    /// </summary>
    public virtual void OnJudgeMiss(ThrowContext context)
    {
    }

    /// <summary>
    /// 던진 뒤 튕겨 나가지 못하고 미끄러지기 시작할 때 특수 동작을 한다. 기본 동작은 없다.
    /// context를 사용하며, 변경하는 상태는 하위 클래스가 정한다.
    /// </summary>
    public virtual void OnSlideStart(ThrowContext context)
    {
    }

    /// <summary>
    /// 던진 뒤 게임오버 전까지 매 프레임 특수 동작을 갱신한다. 기본 동작은 없다.
    /// context와 dt를 사용하며, 변경하는 상태는 하위 클래스가 정한다.
    /// </summary>
    public virtual void UpdateFlight(ThrowContext context, float dt)
    {
    }
}
