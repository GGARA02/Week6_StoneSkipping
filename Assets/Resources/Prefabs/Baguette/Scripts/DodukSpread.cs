using System;

using UnityEngine;

// 자동차에서 소환되어 포물선으로 날아가며 수면에 닿으면 물보라를 일으키는 도둑.
// 머리가 비행 궤적의 진행 방향을 향하는 다이빙 자세로 날아가며 물고기처럼 좌우로 파닥거린다.
public class DodukSpread : MonoBehaviour
{
    private const float WIGGLE_SPEED = 14f;
    private const float WIGGLE_ANGLE = 18f;
    private const float MAX_LIFETIME = 8f;

    [Header("물리")]
    [SerializeField]
    private float _gravityScale = 1f;

    [Header("상태")]
    private SkipEffect _skipEffect;
    private Vector3 _position;
    private Vector3 _velocity;
    private float _gravity;
    private float _waterY;
    private float _airTime;
    private bool _isLaunched;

    void Update()
    {
        if (!_isLaunched) return;

        float dt = Time.deltaTime;
        _airTime += dt;

        // 중력 가속도 적용 및 위치 갱신
        _velocity += Vector3.down * (_gravity * _gravityScale * dt);
        _position += _velocity * dt;

        // 머리가 진행 궤적을 향하는 다이빙 자세로 물고기처럼 파닥거리는 회전 적용
        transform.SetPositionAndRotation(_position, CalculateDiveWiggleRotation());

        // 수면에 닿으면 물보라를 일으키고 제거
        if (_velocity.y < 0f && _position.y <= _waterY)
        {
            HandleWaterLanded();
            return;
        }

        // 비정상적인 체공 방지를 위해 제한 시간 초과 시 제거
        if (_airTime >= MAX_LIFETIME)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 초기 속도와 수면 높이, 물보라 이펙트 참조를 설정하여 비행을 시작한다.
    /// initialVelocity, waterY, skipEffect를 사용하며, 물리 상태와 _isLaunched를 변경한다.
    /// </summary>
    public void Launch(Vector3 initialVelocity, float waterY, SkipEffect skipEffect)
    {
        _position = transform.position;
        _velocity = initialVelocity;
        _waterY = waterY;
        _skipEffect = skipEffect;
        _gravity = -Physics.gravity.y;
        _airTime = 0f;
        _isLaunched = true;
    }

    /// <summary>
    /// 머리가 진행 방향을 향하는 다이빙 자세를 기준으로 좌우로 파닥거리는 회전을 계산한다.
    /// _velocity와 _airTime을 사용하며, 계산된 회전 Quaternion을 반환한다.
    /// </summary>
    private Quaternion CalculateDiveWiggleRotation()
    {
        if (_velocity.sqrMagnitude < 0.001f)
        {
            return transform.rotation;
        }

        // 진행 방향을 향하는 기본 회전 생성
        Quaternion lookRotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);

        // 머리(+Y)가 진행 방향을 향하도록 앞으로 90도 숙인 다이빙 자세 적용
        Quaternion diveOffset = Quaternion.Euler(90f, 0f, 0f);

        // 다이빙 자세에서 좌우로 몸을 흔드는 파닥거림 적용 (등/배 기준 로컬 Z축 회전)
        Quaternion wiggle = Quaternion.Euler(0f, 0f, Mathf.Sin(_airTime * WIGGLE_SPEED) * WIGGLE_ANGLE);

        return lookRotation * diveOffset * wiggle;
    }

    /// <summary>
    /// 수면 착수 시 물보라와 물결을 발생시키고 오브젝트를 파괴한다.
    /// _position과 _skipEffect를 사용하며, 이펙트 재생 후 오브젝트를 소멸시킨다.
    /// </summary>
    private void HandleWaterLanded()
    {
        if (_skipEffect != null)
        {
            Vector3 splashPoint = _position;
            splashPoint.y = _waterY;
            _skipEffect.PlaySplash(splashPoint, 0.35f, 2.5f);
        }

        Destroy(gameObject);
    }
}
