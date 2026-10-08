using UnityEngine;

// 복어의 특수 동작. 던졌을 때 SPACE 판정에 성공하면 한 번 부풀어 수면에 닿는 면적이 커지고, 공기를 머금어 천천히 떨어진다.
public class PufferAbility : FishAbility
{
    [Header("부풀기")]
    [Tooltip("다 부풀었을 때의 몸 배율")]
    [SerializeField]
    private float _inflateScale = 2.2f;
    [Tooltip("부푼 뒤 공중에서 받는 중력 배율. 1보다 작으면 천천히 떨어진다")]
    [SerializeField]
    private float _inflatedGravityScale = 0.3f;

    [Header("상태")]
    private bool _inflated;

    /// <summary>
    /// 던진 복어가 SPACE 판정에 성공하면 한 번만 부풀리고 수면 판정용 형상을 다시 만든 뒤 공중 중력을 줄인다.
    /// context의 몸, 모양 생성기, 플레이어와 _inflateScale, _inflatedGravityScale을 사용하며, judge는 쓰지 않는다.
    /// 몸 크기, 충돌체, 샘플점, 공중 중력 배율, _inflated를 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (_inflated) return;

        _inflated = true;
        context.Body.localScale = Vector3.Scale(context.Body.localScale, InflatedScale(_inflateScale));
        context.Shape.BuildPrefabGeometry();
        context.Player.SetAirGravityScale(_inflatedGravityScale);
    }

    /// <summary>
    /// 부푼 정도로 몸의 로컬 배율을 구한다.
    /// grow를 사용하며, 로컬 배율을 반환한다.
    /// </summary>
    private static Vector3 InflatedScale(float grow)
    {
        // 세운 몸 기준 로컬 x는 높이, y는 두께, z는 길이다.
        return new Vector3(grow, grow * 1.3f, 1f + (grow - 1f) * 0.3f);
    }
}
