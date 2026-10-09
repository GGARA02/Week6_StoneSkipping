using UnityEngine;

// 창문 위치에서 정면과 위쪽 속도를 계산하고 공용 사출 능력에 생성을 요청한다.
public class DodukSpawner : MonoBehaviour
{
    [Header("발사 설정")]
    [Tooltip("창문 밖으로 튀어나가는 기본 발사 속도")]
    [SerializeField]
    private float _launchSpeed = 15f;
    [Tooltip("상향으로 솟구치는 기본 속도")]
    [SerializeField]
    private float _upwardSpeed = 6f;

    /// <summary>
    /// 창문의 전방 방향과 위쪽 속도에 무작위 편차를 적용하여 공용 사출을 요청한다.
    /// context, ability, judge와 발사 설정을 사용하며, 프리팹의 원래 크기로 도둑 Fish를 사출한다.
    /// </summary>
    public void SpawnDoDuk(ThrowContext context, EjectFishAbility ability, SkipJudge judge)
    {
        float speed = Random.Range(0.85f, 1.15f) * _launchSpeed;
        float upward = Random.Range(0.85f, 1.15f) * _upwardSpeed;
        Vector3 launchVelocity = transform.forward * speed + Vector3.up * upward;
        ability.TryEjectFrom(context, judge, transform, launchVelocity, ability.FishPrefab.transform.localScale);
    }
}
