using UnityEngine;

[RequireComponent(typeof(AidePortrait))]
public class AideProjectileAbility : FishAbility
{
    [Header("표정 연결")]
    [SerializeField] private Transform _cardRoot;
    private AidePortrait _portrait;
    private ShipAideController _controller;

    [Header("비행 상태")]
    private bool _throwStarted;

    void Awake()
    {
        _portrait = GetComponent<AidePortrait>();
    }

    /// <summary>
    /// 투척물 UI를 관리하는 controller를 연결한다.
    /// 판정 결과를 전달할 참조를 저장한다.
    /// </summary>
    public void Bind(ShipAideController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// 투척 준비 시 카드와 얇은 판정용 형상을 수면과 나란히 눕힌다.
    /// 연결된 카드 루트의 로컬 회전을 변경해 기존 메시 기반 수면 판정에 사용한다.
    /// </summary>
    public override void OnPrepareThrow()
    {
        _cardRoot.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    /// <summary>
    /// 수면 출현 시 AideJjang의 표정을 Panic으로 변경한다.
    /// 입력 없이 현재 개체의 Portrait 텍스처만 변경한다.
    /// </summary>
    public override void OnWaterSpawn()
    {
        _cardRoot.localRotation = Quaternion.identity;
        _portrait.SetExpression("Panic");
    }

    /// <summary>
    /// context의 실제 비행 시작을 확인해 최초 프레임에 Panic 표정과 대사를 표시한다.
    /// dt와 관계없이 시작 상태를 저장하고 이후 판정 반응은 기존 동작을 따른다.
    /// </summary>
    public override void UpdateFlight(ThrowContext context, float dt)
    {
        if (_throwStarted || !context.Player.IsThrown) return;
        _throwStarted = true;
        ShowPanic();
    }

    /// <summary>
    /// context로 전달된 미끄러짐 시작에 맞춰 마지막 연타 구간의 표정을 Panic으로 바꾼다.
    /// 현재 투척물의 표정과 연결된 말풍선 대사를 갱신한다.
    /// </summary>
    public override void OnSlideStart(ThrowContext context)
    {
        ShowPanic();
    }

    /// <summary>
    /// context와 judge의 성공 통지를 투척물 UI에 전달한다.
    /// 연결된 컨트롤러의 성공 표정과 대사를 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (_controller != null) _controller.ReactSuccess(this);
    }

    /// <summary>
    /// context에서 발생한 SPACE 실패를 투척물 UI에 전달한다.
    /// 실패 누적 없이 컨트롤러의 실패 표정과 대사를 변경한다.
    /// </summary>
    public override void OnJudgeMiss(ThrowContext context)
    {
        if (_controller != null) _controller.ReactFailure(this);
    }

    /// <summary>
    /// 연결된 컨트롤러로 Panic 표정과 대사를 표시하고 연결이 없으면 현재 Portrait만 변경한다.
    /// 입력값 없이 비행 시작 또는 마지막 연타 구간의 표시 상태를 갱신한다.
    /// </summary>
    private void ShowPanic()
    {
        if (_controller != null) _controller.ReactPanic(this);
        else _portrait.SetExpression("Panic");
    }
}
