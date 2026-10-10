using System.Collections;

using UnityEngine;

public class ShipNormalAbility : FishAbility
{
    private const float FLAME_DURATION = 2f;

    [Header("화염 포탑")]
    [SerializeField] private GameObject[] _flameEffects = new GameObject[0];
    private Coroutine _flameRoutine;

    [Header("진행 상태")]
    private ShipAideController _controller;
    private bool _throwStarted;

    void OnDisable()
    {
        if (_flameRoutine != null) StopCoroutine(_flameRoutine);
        _flameRoutine = null;
        SetFlamesActive(false);
    }

    /// <summary>
    /// 현재 투척 선택을 관리하는 controller를 연결한다.
    /// 이후 최초 투척과 판정 결과를 전달할 참조를 저장한다.
    /// </summary>
    public void Bind(ShipAideController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// context의 실제 비행 시작을 확인해 첫 프레임에 ShipNormal 사용을 알린다.
    /// dt와 관계없이 한 번만 시작 상태를 변경하고 연결된 컨트롤러를 호출한다.
    /// </summary>
    public override void UpdateFlight(ThrowContext context, float dt)
    {
        if (_throwStarted || _controller == null || !context.Player.IsThrown) return;
        _throwStarted = true;
        _controller.BeginShipThrow(this);
    }

    /// <summary>
    /// SPACE 성공 및 Mash Space 입력을 초상화 상호작용에 전달한다.
    /// context와 judge의 성공 통지로 표정과 대사를 변경하고 화염 포탑을 2초 동안 켠다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (_controller != null) _controller.ReactSuccess(this);
        if (_flameRoutine != null) StopCoroutine(_flameRoutine);
        _flameRoutine = StartCoroutine(PlayFlames());
    }

    /// <summary>
    /// context에서 발생한 SPACE 실패를 초상화 상호작용에 전달한다.
    /// 연결된 컨트롤러의 실패 누적과 표정, 대사를 변경한다.
    /// </summary>
    public override void OnJudgeMiss(ThrowContext context)
    {
        if (_controller != null) _controller.ReactFailure(this);
    }

    /// <summary>
    /// 연결된 화염 이펙트를 켜고 FLAME_DURATION이 지나면 끄는 코루틴을 반환한다.
    /// 입력값 없이 포탑 표시와 실행 중인 코루틴 참조를 갱신한다.
    /// </summary>
    private IEnumerator PlayFlames()
    {
        SetFlamesActive(true);
        yield return new WaitForSeconds(FLAME_DURATION);
        SetFlamesActive(false);
        _flameRoutine = null;
    }

    /// <summary>
    /// active를 사용해 프리팹에 연결된 모든 화염 이펙트의 활성 상태를 변경한다.
    /// 켜질 때 파티클이 재생되고 꺼질 때 화면에서 제거된다.
    /// </summary>
    private void SetFlamesActive(bool active)
    {
        foreach (GameObject effect in _flameEffects) effect.SetActive(active);
    }
}
