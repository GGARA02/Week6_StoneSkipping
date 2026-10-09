using UnityEngine;

public class EjectFishAbility : FishAbility
{
    [Header("사출 대상")]
    [SerializeField] private Fish _fishPrefab;
    [SerializeField] private Transform _origin;

    [Header("성공 조건")]
    [SerializeField] private bool _perfectOnly;
    [Tooltip("한 번 던지는 동안의 최대 사출 횟수. 0이면 제한하지 않는다")]
    [Min(0)]
    [SerializeField] private int _maxEjections;
    private int _ejectionCount;

    [Header("사출 속도")]
    [Tooltip("X/Z는 사출 기준의 방향이며, Y는 수면 기준 위쪽 속도다")]
    [SerializeField] private Vector3 _localVelocity = new Vector3(0f, 12f, 0f);
    [SerializeField] private float _inheritVelocity = 0.35f;

    public Fish FishPrefab => _fishPrefab;

    /// <summary>
    /// 성공 판정과 사출 횟수 조건을 만족하면 기준 위치에서 물고기를 사출한다.
    /// context와 judge, Inspector 속도를 사용하며, 성공한 사출 횟수를 증가시킨다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (context.Player.IsSliding) return;
        if (_perfectOnly && judge != SkipJudge.Perfect) return;
        if (_maxEjections > 0 && _ejectionCount >= _maxEjections) return;

        Vector3 velocity = _origin.TransformDirection(new Vector3(_localVelocity.x, 0f, _localVelocity.z))
            + Vector3.up * _localVelocity.y
            + context.Player.Velocity * _inheritVelocity;
        if (context.Spawner.TryEject(_fishPrefab, _origin.position, _origin.rotation, _origin.lossyScale, velocity))
        {
            _ejectionCount++;
        }
    }
}
