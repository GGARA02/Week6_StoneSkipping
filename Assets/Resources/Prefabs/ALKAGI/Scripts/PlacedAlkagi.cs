using System.Collections.Generic;

using UnityEngine;

public class PlacedAlkagi : MonoBehaviour
{
    private enum AttackPhase { Idle, Lowering, Sweeping, Recovering }

    [Header("참조")]
    [SerializeField] private FishSpawner _spawner;
    [SerializeField] private Fish _fishPrefab;
    [SerializeField] private Transform _bodyPivot;
    [SerializeField] private PlacedAlkagiLaser _laser;
    [SerializeField] private AlkagiWaterHole _holePrefab;
    private PlayerController _player;
    private Quaternion _initialRotation;
    private AlkagiWaterHole _activeHole;
    private readonly List<AlkagiWaterHole> _holes = new List<AlkagiWaterHole>();

    [Header("공격")]
    [SerializeField, Min(0f)] private float _detectionRange = 40f;
    [Tooltip("대기 자세로 돌아온 뒤 다음 공격까지 기다리는 시간")]
    [SerializeField, Min(0.01f)] private float _attackInterval = 4f;
    [SerializeField, Min(0.01f)] private float _lowerDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float _sweepDuration = 2.5f;
    [SerializeField, Min(0.01f)] private float _recoverDuration = 0.5f;
    private AttackPhase _phase;
    private float _phaseTime;
    private float _cooldown;
    private bool _caught;

    [Header("물 구멍")]
    [SerializeField] private float _waterSurfaceOffset = 0.02f;
    [SerializeField, Min(1f)] private float _holeDepth = 200f;

    [Header("재등장")]
    [SerializeField] private bool _respawnOnRestart = true;

    void Awake()
    {
        if (_spawner == null) _spawner = FindFirstObjectByType<FishSpawner>();
        _initialRotation = transform.rotation;
        _spawner.OnCleared += HandleCleared;
        _laser.OnWaterContact += HandleWaterContact;
    }

    void Start()
    {
        _player = _spawner.PlayerBody.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!_player.IsThrown || _player.IsGameOver)
        {
            if (_phase != AttackPhase.Idle) ResetAttack();
            return;
        }
        float dt = Time.deltaTime;
        if (_phase == AttackPhase.Idle)
        {
            _cooldown = Mathf.Max(0f, _cooldown - dt);
            if (_cooldown > 0f || Vector3.Distance(transform.position, _spawner.PlayerBody.position) > _detectionRange) return;
            Vector3 direction = _spawner.PlayerBody.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
            SetPhase(AttackPhase.Lowering);
        }
        _phaseTime += dt;
        switch (_phase)
        {
            case AttackPhase.Lowering:
                SetPitch(Mathf.Lerp(0f, 60f, Mathf.SmoothStep(0f, 1f, _phaseTime / _lowerDuration)));
                if (_phaseTime < _lowerDuration) break;
                _laser.BeginFire(_detectionRange);
                SetPhase(AttackPhase.Sweeping);
                CheckLaser();
                break;
            case AttackPhase.Sweeping:
                SetPitch(Mathf.Lerp(60f, -30f, Mathf.Clamp01(_phaseTime / _sweepDuration)));
                CheckLaser();
                if (_phaseTime < _sweepDuration) break;
                EndFire();
                if (_caught)
                {
                    gameObject.SetActive(false);
                    return;
                }
                SetPhase(AttackPhase.Recovering);
                break;
            case AttackPhase.Recovering:
                SetPitch(Mathf.Lerp(-30f, 0f, Mathf.SmoothStep(0f, 1f, _phaseTime / _recoverDuration)));
                if (_phaseTime < _recoverDuration) break;
                SetPhase(AttackPhase.Idle);
                _cooldown = _attackInterval;
                break;
        }
    }

    void OnDisable()
    {
        ResetAttack();
    }

    void OnDestroy()
    {
        if (_spawner != null) _spawner.OnCleared -= HandleCleared;
        _laser.OnWaterContact -= HandleWaterContact;
        ClearHoles();
    }

    /// <summary>
    /// phase로 공격 단계를 전환하고 단계 경과 시간을 초기화한다.
    /// 입력된 단계로 _phase와 _phaseTime을 변경한다.
    /// </summary>
    private void SetPhase(AttackPhase phase)
    {
        _phase = phase;
        _phaseTime = 0f;
    }

    /// <summary>
    /// pitch 각도로 몸체 피벗의 로컬 X 회전을 설정한다.
    /// 입력 각도를 사용하며 위치와 수평 조준은 유지한다.
    /// </summary>
    private void SetPitch(float pitch)
    {
        _bodyPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>
    /// 플레이어 몸과 빔의 접촉을 검사하고 최초 접촉에서 원본 ALKAGI를 포획 등록한다.
    /// 접촉 시 포획 상태를 저장하여 중복 등록을 막고 몸체 올림 완료 후 숨길 수 있게 한다.
    /// </summary>
    private void CheckLaser()
    {
        if (!_laser.Sweep(_spawner.PlayerBody, _player.WaterContactPoint.y) || _caught) return;
        _caught = true;
        _spawner.HandlePlacedFishCaught(_fishPrefab.Type, _spawner.PlayerBody.position);
    }

    /// <summary>
    /// 수면 접촉점 point에 최초 구멍을 만들고 레이저가 도달한 지점까지 길이를 확장한다.
    /// 빔 너비와 깊이 및 수면 오프셋을 사용하며 활성 구멍과 정리 목록을 변경한다.
    /// </summary>
    private void HandleWaterContact(Vector3 point)
    {
        point.y += _waterSurfaceOffset;
        if (_activeHole == null)
        {
            _activeHole = Instantiate(_holePrefab, point, transform.rotation);
            _activeHole.Initialize(point, _laser.Width, _holeDepth);
            _holes.RemoveAll(hole => hole == null);
            _holes.Add(_activeHole);
        }
        _activeHole.ExtendTo(point);
    }

    /// <summary>
    /// 빔을 끄고 이번 공격의 물 구멍에 종료 후 축소를 요청한다.
    /// 입력값 없이 레이저 표시와 활성 구멍 참조를 변경한다.
    /// </summary>
    private void EndFire()
    {
        _laser.EndFire();
        if (_activeHole == null) return;
        _activeHole.BeginClosing();
        _activeHole = null;
    }

    /// <summary>
    /// 진행 중인 공격을 종료하고 몸체를 대기 자세로 복원한다.
    /// 입력값 없이 단계와 발사 대기 시간을 초기화한다.
    /// </summary>
    private void ResetAttack()
    {
        EndFire();
        SetPitch(0f);
        SetPhase(AttackPhase.Idle);
        _cooldown = _attackInterval;
    }

    /// <summary>
    /// 재시작 시 이전 구멍을 제거하고 설정에 따라 포획된 ALKAGI를 재등장시킨다.
    /// _respawnOnRestart를 사용하며 공격 상태와 초기 방향을 복원한다.
    /// </summary>
    private void HandleCleared()
    {
        ClearHoles();
        ResetAttack();
        transform.rotation = _initialRotation;
        if (!_respawnOnRestart) return;
        _caught = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 이 ALKAGI가 생성한 남아 있는 물 구멍을 제거한다.
    /// 저장 목록을 사용하며 구멍 오브젝트와 목록을 정리한다.
    /// </summary>
    private void ClearHoles()
    {
        foreach (AlkagiWaterHole hole in _holes)
        {
            if (hole != null) Destroy(hole.gameObject);
        }
        _holes.Clear();
        _activeHole = null;
    }
}
