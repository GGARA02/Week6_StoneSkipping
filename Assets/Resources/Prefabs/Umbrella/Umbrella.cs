using UnityEngine;

public class Umbrella : FishAbility
{
    [Header("참조")]
    [SerializeField] private Renderer _bodyRenderer;
    [SerializeField] private Material _umbrellaOn;
    [SerializeField] private Material _umbrellaOff;

    [Header("던질 때 회전")]
    [SerializeField] private Vector3 _throwRotation = new Vector3(-90f, 180f, 0f);

    [Header("펼친 상태")]
    [Range(0.01f, 1f)]
    [SerializeField] private float _openGravityScale = 0.3f;

    void Awake()
    {
        _bodyRenderer.sharedMaterial = _umbrellaOff;
    }

    void Start()
    {
        // 던질 프리팹은 생성기가 Fish를 끈 뒤 기본 자세와 충돌체를 설정한다.
        if (GetComponent<Fish>().enabled) return;

        transform.localRotation = Quaternion.Euler(_throwRotation) * transform.localRotation;
        transform.parent.GetComponent<FishMeshGenerator>().BuildPrefabGeometry();
    }

    /// <summary>
    /// 수면 타이밍 판정에 성공하면 우산을 펼치고 공중 중력을 줄인다. 미끄러짐 입력은 제외한다.
    /// context의 플레이어와 judge의 성공 콜백을 사용하며, 몸 머티리얼과 공중 중력 배율을 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (context.Player.IsSliding) return;

        SetOpen(context, true);
    }

    /// <summary>
    /// 수면 타이밍 판정에 실패하면 우산을 접고 원래 공중 중력으로 되돌린다.
    /// context의 플레이어를 사용하며, 몸 머티리얼과 공중 중력 배율을 변경한다.
    /// </summary>
    public override void OnJudgeMiss(ThrowContext context)
    {
        SetOpen(context, false);
    }

    /// <summary>
    /// 우산의 펼침 여부에 맞춰 표시와 낙하 중력을 함께 설정한다.
    /// context, open과 Inspector 설정을 사용하며, 몸 머티리얼과 플레이어의 공중 중력 배율을 변경한다.
    /// </summary>
    private void SetOpen(ThrowContext context, bool open)
    {
        _bodyRenderer.sharedMaterial = open ? _umbrellaOn : _umbrellaOff;
        context.Player.SetAirGravityScale(open ? _openGravityScale : 1f);
    }
}
