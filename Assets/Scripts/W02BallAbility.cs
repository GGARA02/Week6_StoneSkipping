using UnityEngine;

public class W02BallAbility : FishAbility
{
    [Header("크기")]
    [Min(0.01f)]
    [SerializeField] private float _ejectedScaleMultiplier = 10f;
    [Min(0.01f)]
    [SerializeField] private float _waterSpawnScaleMultiplier = 20f;
    [Min(0.01f)]
    [SerializeField] private float _throwScaleMultiplier = 20f;

    [Header("사출 방향")]
    [Tooltip("수평 사출 방향을 카메라 정면으로 보정하는 비율. 위쪽 도약 속도는 유지한다")]
    [Range(0f, 1f)]
    [SerializeField] private float _cameraDirectionCorrection = 0.5f;

    /// <summary>
    /// 던질 개체의 원래 크기를 설정 배율로 확대한다.
    /// _throwScaleMultiplier를 사용하며, 충돌체 생성 전 개체의 스케일을 변경한다.
    /// </summary>
    public override void OnPrepareThrow()
    {
        transform.localScale *= _throwScaleMultiplier;
    }

    /// <summary>
    /// 최초 사출 크기를 설정 배율로 확대하고 카메라의 수평 방향으로 속도를 보정한다.
    /// velocity와 확대 및 보정 설정을 사용하며, 스케일을 변경하고 보정된 속도를 반환한다.
    /// </summary>
    public override Vector3 OnEjected(Vector3 velocity)
    {
        transform.localScale *= _ejectedScaleMultiplier;
        Camera camera = Camera.main;
        if (camera == null || _cameraDirectionCorrection <= 0f) return velocity;

        Vector3 horizontal = Vector3.ProjectOnPlane(velocity, Vector3.up);
        Vector3 cameraForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
        if (horizontal.sqrMagnitude <= 0.0001f || cameraForward.sqrMagnitude <= 0.0001f) return velocity;

        Vector3 direction = Vector3.Slerp(horizontal.normalized, cameraForward.normalized, _cameraDirectionCorrection);
        return direction * horizontal.magnitude + Vector3.up * velocity.y;
    }

    /// <summary>
    /// 수면 출현용 프리팹의 원래 크기를 설정 배율로 확대한다.
    /// _waterSpawnScaleMultiplier를 사용하며, 이 개체의 스케일을 변경한다.
    /// </summary>
    public override void OnWaterSpawn()
    {
        transform.localScale *= _waterSpawnScaleMultiplier;
    }
}
