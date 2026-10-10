using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

public class FisherQteController : MonoBehaviour
{
    private const string SUCCESS_KEY = "SkipStoneV2.FisherQteSuccess";
    private const float DURATION = 5f;
    private const int FIRST_INPUT_COUNT = 4;
    private const int SECOND_INPUT_COUNT = 10;
    private const int FINAL_INPUT_COUNT = 100;
    private const float SUCCESS_SPIN_DURATION = 0.3f;

    private static readonly Key[] _firstKeys = { Key.W, Key.A, Key.S, Key.D };
    private static readonly Key[] _secondKeys = { Key.Q, Key.W, Key.E, Key.R, Key.A, Key.S, Key.D, Key.F };
    private static readonly Key[] _alphabetKeys =
    {
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I,
        Key.J, Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R,
        Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
    };

    [Header("참조")]
    private FishSpawner _spawner;
    private PlayerController _player;
    private Fish _fish;

    [Header("QTE 상태")]
    private Key[] _sequence;
    private Vector3 _incomingVelocity;
    private Quaternion _rotationBeforeQte;
    private float _deadline;
    private float _previousTimeScale;
    private int _inputIndex;
    private int _startedFrame;
    private bool _isInitialized;
    private bool _isActive;
    private bool _isResolving;

    [Header("화면 표시")]
    [SerializeField] private FisherQteView _uiPrefab;
    private FisherQteView _ui;

    public bool IsResolving => _isResolving;

    void Update()
    {
        if (!_isActive) return;
        if (_player.IsGameOver)
        {
            Cancel();
            return;
        }
        if (_isResolving) return;
        if (Time.unscaledTime >= _deadline)
        {
            StartCoroutine(Resolve(false));
            return;
        }
        if (Time.frameCount == _startedFrame) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard[_sequence[_inputIndex]].wasPressedThisFrame) return;

        _inputIndex++;
        if (_inputIndex == _sequence.Length) StartCoroutine(Resolve(true));
    }

    void LateUpdate()
    {
        if (_isActive && !_isResolving)
            _ui.SetProgress(_inputIndex, (_deadline - Time.unscaledTime) / DURATION);
    }

    void OnDestroy()
    {
        if (_ui != null) Destroy(_ui.gameObject);
    }

    void OnDisable()
    {
        Cancel();
        if (!_isInitialized) return;
        _spawner.OnClearing -= Cancel;
        _isInitialized = false;
    }

    /// <summary>
    /// fish와 spawner를 사용해 자연 출현한 33Fisher의 QTE 참조를 연결한다.
    /// 플레이어와 접촉 대상을 저장하고 재시작 시 취소 이벤트를 구독한다.
    /// </summary>
    public void Initialize(Fish fish, FishSpawner spawner)
    {
        if (_isInitialized) _spawner.OnClearing -= Cancel;
        _fish = fish;
        _spawner = spawner;
        _player = spawner.PlayerBody.GetComponent<PlayerController>();
        _spawner.OnClearing += Cancel;
        _isInitialized = true;
    }

    /// <summary>
    /// 입력값 없이 저장된 성공 횟수로 무작위 키 순서를 만들고 실제 시간 5초 QTE를 시작한다.
    /// 플레이어의 접촉 속도와 기존 시간 배율을 저장하며 시작 여부를 반환한다.
    /// </summary>
    public bool TryBegin()
    {
        if (_isActive || !_isInitialized || !isActiveAndEnabled || _fish.IsEjected
            || !_player.IsThrown || _player.IsGameOver || _spawner.ScreenCapture.IsCapturing
            || Time.timeScale <= 0f) return false;

        int successes = PlayerPrefs.GetInt(SUCCESS_KEY, 0);
        Key[] keys = successes == 0 ? _firstKeys : successes == 1 ? _secondKeys : _alphabetKeys;
        int count = successes == 0 ? FIRST_INPUT_COUNT : successes == 1 ? SECOND_INPUT_COUNT : FINAL_INPUT_COUNT;
        _sequence = new Key[count];
        for (int i = 0; i < count; i++) _sequence[i] = keys[Random.Range(0, keys.Length)];

        _incomingVelocity = _spawner.PlayerBody.linearVelocity;
        _rotationBeforeQte = transform.rotation;
        _inputIndex = 0;
        _startedFrame = Time.frameCount;
        _deadline = Time.unscaledTime + DURATION;
        _previousTimeScale = Time.timeScale;
        if (_ui == null) _ui = Instantiate(_uiPrefab);
        _ui.Show(_sequence, successes);
        _isActive = true;
        Time.timeScale = 0f;
        return true;
    }

    /// <summary>
    /// 입력값 없이 진행 중인 QTE와 결과 대기를 취소하고 기존 시간 배율을 복원한다.
    /// 재시작 또는 비활성화 시 호출되며 성공 횟수와 획득 상태는 변경하지 않는다.
    /// </summary>
    private void Cancel()
    {
        if (!_isActive) return;
        StopAllCoroutines();
        _ui.Hide();
        transform.rotation = _rotationBeforeQte;
        _isActive = false;
        _isResolving = false;
        Time.timeScale = _previousTimeScale;
    }

    /// <summary>
    /// success를 사용해 QTE 결과를 적용하는 코루틴을 반환한다.
    /// 시간 배율을 복원하고 성공 시 반사 속도와 기록을 즉시 갱신한 뒤 회전하며 실패 시 획득을 처리한다.
    /// </summary>
    private IEnumerator Resolve(bool success)
    {
        _isResolving = true;
        _ui.Hide();
        // 결과 입력이 일반 조작으로 전달되지 않도록 다음 Update까지 시간 정지를 유지한다.
        yield return null;
        Time.timeScale = _previousTimeScale;
        if (success)
        {
            _player.ReboundFromFisher(_incomingVelocity);
            PlayerPrefs.SetInt(SUCCESS_KEY, PlayerPrefs.GetInt(SUCCESS_KEY, 0) + 1);
            PlayerPrefs.Save();
            yield return SpinBeforeRebound();
        }
        _isActive = false;
        _isResolving = false;
        if (!success)
        {
            _spawner.HandleFishCaught(_fish);
        }
    }

    /// <summary>
    /// 실제 경과 시간과 QTE 시작 회전을 사용해 수직축 기준 한 바퀴 회전하는 코루틴을 반환한다.
    /// 게임 진행 중 SUCCESS_SPIN_DURATION 동안 회전하고 마지막에 원래 회전을 복원한다.
    /// </summary>
    private IEnumerator SpinBeforeRebound()
    {
        float elapsed = 0f;
        while (elapsed < SUCCESS_SPIN_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float angle = 360f * Mathf.Clamp01(elapsed / SUCCESS_SPIN_DURATION);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up) * _rotationBeforeQte;
            yield return null;
        }
        transform.rotation = _rotationBeforeQte;
    }

}
