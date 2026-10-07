using UnityEngine;

// 조작 없이 알아서 따라가는 카메라. 진행 방향 뒤에서 보고, 빠를수록 멀리서 앞을 더 보여주고,
// 돌이 높이 뜨면 각도를 높여 앞쪽 수면이 보이게 한다. 튕김 흔들림과 속도에 따른 FOV도 처리한다.
public class CameraControllerV2 : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    private Transform _focalPoint;
    [SerializeField]
    private GameObject _water;
    private Camera _camera;
    private Rigidbody _targetBody;
    private float _waterY;

    [Header("자동 추적")]
    [Tooltip("느릴 때 / 가장 빠를 때 돌과의 거리")]
    [SerializeField]
    private Vector2 _distanceRange = new Vector2(16f, 26f);
    [Tooltip("이 수평 속도(m/s)에서 거리, 앞보기, FOV가 최대가 된다")]
    [SerializeField]
    private float _fullSpeed = 60f;
    [Tooltip("가장 빠를 때 돌보다 얼마나 앞을 바라볼지(m)")]
    [SerializeField]
    private float _lookAhead = 14f;
    [SerializeField]
    private float _basePitch = 14f;
    [Tooltip("돌이 수면 위로 1m 뜰 때마다 높아지는 내려다보는 각도(도)")]
    [SerializeField]
    private float _pitchPerHeight = 1.2f;
    [SerializeField]
    private float _minPitch = 8f;
    [SerializeField]
    private float _maxPitch = 40f;
    [Tooltip("진행 방향을 따라 도는 빠르기")]
    [SerializeField]
    private float _yawFollowSpeed = 3f;
    [Tooltip("각도와 거리가 바뀌는 빠르기")]
    [SerializeField]
    private float _framingSpeed = 2f;
    [Tooltip("돌을 따라가는 부드러움 (작을수록 딱 붙음)")]
    [SerializeField]
    private float _followSmoothTime = 0.12f;
    [SerializeField]
    private float _minHeightAboveWater = 2f;
    private float _yaw;
    private float _pitch;
    private float _distance;
    private Vector3 _focus;
    private Vector3 _focusVelocity;

    [Header("속도감")]
    [SerializeField]
    private float _baseFov = 60f;
    [SerializeField]
    private float _maxExtraFov = 12f;
    private float _smoothedFov;

    [Header("튕김 흔들림")]
    [SerializeField]
    private float _shakeDuration = 0.25f;
    [SerializeField]
    private float _shakeAmplitude = 0.35f;
    [Tooltip("튕길 때 순간적으로 넓어지는 FOV")]
    [SerializeField]
    private float _punchFov = 6f;
    private float _punchTime = -10f;
    private float _punchStrength;

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        Vector3 velocity = _targetBody.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float speed01 = Mathf.Clamp01(horizontal.magnitude / Mathf.Max(_fullSpeed, 0.01f));
        float follow = 1f - Mathf.Exp(-_yawFollowSpeed * dt);
        float framing = 1f - Mathf.Exp(-_framingSpeed * dt);

        // 움직일 때만 진행 방향 뒤로 돈다. 멈춰 있으면 지금 방향을 유지한다.
        if (horizontal.sqrMagnitude > 1f)
        {
            float targetYaw = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
            _yaw = Mathf.LerpAngle(_yaw, targetYaw, follow);
        }

        float height = _focalPoint.position.y - _waterY;
        float targetPitch = Mathf.Clamp(_basePitch + Mathf.Max(0f, height) * _pitchPerHeight, _minPitch, _maxPitch);
        _pitch = Mathf.Lerp(_pitch, targetPitch, framing);
        _distance = Mathf.Lerp(_distance, Mathf.Lerp(_distanceRange.x, _distanceRange.y, speed01), framing);

        Vector3 ahead = horizontal.sqrMagnitude > 1f ? horizontal.normalized * (_lookAhead * speed01) : Vector3.zero;
        _focus = Vector3.SmoothDamp(_focus, _focalPoint.position + ahead, ref _focusVelocity, _followSmoothTime, Mathf.Infinity, dt);
        UpdateTransform();

        float punch = PunchAmount();
        transform.position += Random.insideUnitSphere * (_shakeAmplitude * punch);

        _smoothedFov = Mathf.Lerp(_smoothedFov, _baseFov + _maxExtraFov * speed01, 1f - Mathf.Exp(-4f * dt));
        _camera.fieldOfView = _smoothedFov + _punchFov * punch;
    }

    /// <summary>
    /// 대상과 물 높이를 준비하고 카메라를 돌 뒤로 맞춘다.
    /// 입력값은 없으며, 카메라 참조와 _waterY, 카메라 위치를 변경한다.
    /// </summary>
    public void Initialize()
    {
        _camera = GetComponent<Camera>();
        _targetBody = _focalPoint.GetComponent<Rigidbody>();
        _waterY = _water.GetComponent<Collider>().bounds.max.y;
        SnapBehindTarget();
    }

    /// <summary>
    /// 카메라를 대상 뒤 기본 각도와 거리로 즉시 옮기고 흔들림과 FOV를 초기화한다.
    /// 대상의 현재 위치와 방향을 사용하며, _yaw, _pitch, _distance, _focus, 카메라 위치와 FOV를 변경한다.
    /// </summary>
    public void SnapBehindTarget()
    {
        _yaw = _focalPoint.eulerAngles.y;
        _pitch = _basePitch;
        _distance = _distanceRange.x;
        _focus = _focalPoint.position;
        _focusVelocity = Vector3.zero;
        _punchTime = -10f;
        _smoothedFov = _baseFov;
        _camera.fieldOfView = _baseFov;
        UpdateTransform();
    }

    /// <summary>
    /// 튕김 순간 카메라 흔들림과 FOV 펀치를 시작한다.
    /// strength(0~1 권장)를 사용하며, _punchTime과 _punchStrength를 변경한다.
    /// </summary>
    public void Punch(float strength)
    {
        _punchTime = Time.unscaledTime;
        _punchStrength = strength;
    }

    /// <summary>
    /// 현재 흔들림 세기를 구한다. 시작 직후 가장 세고 지속 시간 동안 줄어든다.
    /// _punchTime, _punchStrength, _shakeDuration을 사용하며, 0 이상 세기를 반환한다.
    /// </summary>
    private float PunchAmount()
    {
        float t = (Time.unscaledTime - _punchTime) / Mathf.Max(_shakeDuration, 0.01f);
        if (t >= 1f) return 0f;
        float fade = 1f - t;
        return _punchStrength * fade * fade;
    }

    /// <summary>
    /// 궤도 각도와 초점, 거리로 카메라 위치를 정하고 초점을 바라보게 한다.
    /// _yaw, _pitch, _focus, _distance를 사용하며, transform 위치와 회전을 변경한다.
    /// </summary>
    private void UpdateTransform()
    {
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 position = _focus + rotation * Vector3.back * _distance;
        // 카메라가 물속으로 들어가지 않게 한다.
        position.y = Mathf.Max(position.y, _waterY + _minHeightAboveWater);
        transform.position = position;
        transform.LookAt(_focus);
    }
}
