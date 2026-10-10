using UnityEngine;

public class BlackHoleAbility : FishAbility
{
    [Header("물리 형상")]
    [SerializeField] private MeshFilter _throwGeometry;

    [Header("투척 회전")]
    [SerializeField] private float _throwPitch = 0.2f;
    private Transform _body;
    private Quaternion _throwRotation;
    private bool _isThrown;

    public override MeshFilter ThrowGeometry => _throwGeometry;

    /// <summary>
    /// 입력값 없이 새 투척을 준비하고 화면 회전 고정 상태를 초기화한다.
    /// 물리 형상은 Inspector의 본체 메시를 사용한다.
    /// </summary>
    public override void OnPrepareThrow()
    {
        _isThrown = false;
    }

    /// <summary>
    /// 첫 비행 갱신에서 카메라 축과 _throwPitch(도)로 회전을 기록하고 외형에만 적용한다.
    /// context.Body를 사용하며, dt와 무관하게 부모 Rigidbody의 물리 회전은 유지한다.
    /// </summary>
    public override void UpdateFlight(ThrowContext context, float dt)
    {
        if (!_isThrown)
        {
            Transform cameraTransform = Camera.main.transform;
            _body = context.Body;
            _throwRotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up)
                * Quaternion.Euler(_throwPitch, 0f, 0f);
            _isThrown = true;
        }

        _body.rotation = _throwRotation;
    }

    void LateUpdate()
    {
        if (_isThrown)
        {
            _body.rotation = _throwRotation;
        }
    }
}
