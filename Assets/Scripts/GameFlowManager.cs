using UnityEngine;
using UnityEngine.InputSystem;

// 던지기, 비행, 게임오버(상점), 재시작 흐름과 HUD를 담당한다.
// 게임오버 시 씬을 다시 불러오지 않고 제자리에서 리셋하고 새 돌을 만든다.
public class GameFlowManager : MonoBehaviour
{
    private enum State
    {
        Ready,
        Flying,
        GameOver,
    }

    private const string BEST_SKIPS_KEY = "SkipStoneV2.BestSkips";
    private const string BEST_DISTANCE_KEY = "SkipStoneV2.BestDistance";
    private const float POPUP_DURATION = 1.1f;
    private const int STONE_INDEX = -1;
    // 게임오버 직후 SPACE 연타로 바로 재시작되지 않게 기다리는 시간(초)
    private const float RETRY_INPUT_DELAY = 0.6f;

    [Header("참조")]
    [SerializeField]
    private PlayerController _playerController;
    [SerializeField]
    private CameraController _cameraController;
    [SerializeField]
    private StoneMeshGenerator _stoneGenerator;
    [SerializeField]
    private PlayerProgress _progress;
    [SerializeField]
    private FishSpawner _fishSpawner;
    private InputSystem_Actions _inputActions;

    [Header("흐름")]
    private State _state;
    private GameOverReason _gameOverReason;
    private float _gameOverTime;
    private int _bestSkips;
    private float _bestDistance;
    // -1은 돌, 0 이상은 FishSpawner.FishTypes 인덱스
    private int _projectileIndex = STONE_INDEX;
    private int _perfectCount;
    private int _goodCount;
    private int _missCount;

    [Header("HUD")]
    [SerializeField]
    private bool _showHud = true;
    [SerializeField]
    private bool _showDebug = true;
    [Tooltip("수면에 닿기 이 시간(초) 전부터 SPACE 안내를 띄운다")]
    [SerializeField]
    private float _spaceHintLeadTime = 0.6f;
    [Tooltip("판정 문구 세로 위치 (화면 높이 비율). 돌이 화면 가운데 있으므로 위쪽에 둔다")]
    [Range(0f, 1f)]
    [SerializeField]
    private float _popupHeight = 0.13f;
    [Tooltip("SPACE 안내 세로 위치 (화면 높이 비율)")]
    [Range(0f, 1f)]
    [SerializeField]
    private float _spaceHintHeight = 0.8f;
    private string _popupText;
    private Color _popupColor;
    private float _popupTime = -10f;
    private GUIStyle _bigStyle;
    private GUIStyle _mediumStyle;
    private GUIStyle _smallStyle;
    private GUIStyle _centerStyle;
    private GUIStyle _centerSubStyle;
    private GUIStyle _popupStyle;
    private GUIStyle _debugStyle;
    private GUIStyle _buttonStyle;
    private float _styleScale = -1f;

    void Start()
    {
        LockCursor(true);

        _inputActions = new InputSystem_Actions();
        _inputActions.UI.Enable();
        _inputActions.Player.Enable();

        _playerController.Initialize(_inputActions);
        _playerController.OnSkip += HandleSkip;
        _playerController.OnJudge += HandleJudge;
        _playerController.OnGameOver += HandleGameOver;
        _playerController.OnWaterContact += HandleWaterContact;
        _fishSpawner.OnFishCaught += HandleFishCaught;
        _cameraController.Initialize();

        _bestSkips = PlayerPrefs.GetInt(BEST_SKIPS_KEY, 0);
        _bestDistance = PlayerPrefs.GetFloat(BEST_DISTANCE_KEY, 0f);
        _state = State.Ready;
    }

    void OnDestroy()
    {
        _playerController.OnSkip -= HandleSkip;
        _playerController.OnJudge -= HandleJudge;
        _playerController.OnGameOver -= HandleGameOver;
        _playerController.OnWaterContact -= HandleWaterContact;
        _fishSpawner.OnFishCaught -= HandleFishCaught;
        _inputActions.Dispose();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            LockCursor(Cursor.lockState != CursorLockMode.Locked);
        }
        bool restartPressed = (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            || (gamepad != null && (gamepad.buttonNorth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame));
        if (restartPressed)
        {
            Restart();
            return;
        }

        // SPACE와 게임패드 A는 Jump 액션에 묶여 있다.
        bool jumpPressed = _inputActions.Player.Jump.WasPressedThisFrame();
        if (_state == State.GameOver)
        {
            UpdateShopInput(gamepad, jumpPressed);
            return;
        }
        if (_state != State.Ready) return;

        if ((keyboard != null && keyboard.qKey.wasPressedThisFrame) || (gamepad != null && gamepad.leftShoulder.wasPressedThisFrame))
        {
            CycleProjectile(-1);
        }
        if ((keyboard != null && keyboard.eKey.wasPressedThisFrame) || (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame))
        {
            CycleProjectile(1);
        }

        if (jumpPressed)
        {
            Throw();
            return;
        }
        if (!_inputActions.UI.Click.WasPressedThisFrame()) return;

        // 커서가 풀려 있을 때의 클릭은 다시 잠그는 용도로만 쓴다.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
            return;
        }
        Throw();
    }

    void OnGUI()
    {
        if (!_showHud) return;

        EnsureStyles();
        float s = _styleScale;
        float width = Screen.width;
        float height = Screen.height;

        ShadowLabel(new Rect(24f * s, 16f * s, 600f * s, 64f * s), $"SKIP  {_playerController.SkipCount}", _bigStyle, Color.white);
        ShadowLabel(new Rect(24f * s, 76f * s, 600f * s, 44f * s), $"{_playerController.Distance:0.0} m", _mediumStyle, Color.white);
        ShadowLabel(new Rect(24f * s, 118f * s, 800f * s, 36f * s), $"BEST  {_bestSkips} skips / {_bestDistance:0.0} m", _smallStyle, new Color(1f, 1f, 1f, 0.75f));
        ShadowLabel(new Rect(24f * s, 150f * s, 800f * s, 36f * s), $"{_progress.Money} G", _smallStyle, new Color(1f, 0.85f, 0.3f));

        if (_state == State.Ready)
        {
            DrawReady(width, height, s);
        }
        else if (_state == State.GameOver)
        {
            DrawShop(width, height, s);
        }
        else if (_playerController.IsSliding && _playerController.SlidePower > 0f)
        {
            // 미끄러지는 동안은 남은 밀기 힘만큼 진하게 연타 안내를 띄운다.
            Color slideColor = new Color(1f, 1f, 1f, 0.35f + 0.65f * _playerController.SlidePower);
            ShadowLabel(new Rect(0f, height * _spaceHintHeight, width, 80f * s), "MASH SPACE!", _centerStyle, slideColor);
        }
        else if (_playerController.CanJudge && _playerController.TimeToWaterImpact <= _spaceHintLeadTime)
        {
            // 닿을수록 진해지고, GOOD 이상 구간에 들어오면 금색으로 바뀐다.
            float approach = 1f - Mathf.Clamp01(_playerController.TimeToWaterImpact / Mathf.Max(_spaceHintLeadTime, 0.01f));
            Color hintColor = _playerController.IsInJudgeWindow
                ? new Color(1f, 0.85f, 0.25f, 1f)
                : new Color(1f, 1f, 1f, 0.35f + 0.5f * approach);
            ShadowLabel(new Rect(0f, height * _spaceHintHeight, width, 80f * s), "SPACE", _centerStyle, hintColor);
        }

        // 게임오버 화면에서는 제목과 겹치지 않도록 팝업을 그리지 않는다.
        float popupAge = Time.unscaledTime - _popupTime;
        if (_state != State.GameOver && _popupText != null && popupAge < POPUP_DURATION)
        {
            float t = popupAge / POPUP_DURATION;
            Color color = _popupColor;
            color.a = 1f - t * t;
            float rise = 30f * s * t;
            ShadowLabel(new Rect(0f, height * _popupHeight - rise, width, 80f * s), _popupText, _popupStyle, color);
        }

        if (_showDebug)
        {
            string debug =
                $"speed {_playerController.Speed:0.0} m/s\n" +
                $"tilt {_playerController.CurrentPitch:0} / {_playerController.TargetPitch:0} deg  roll {_playerController.TargetRoll:0}\n" +
                $"spin {_playerController.SpinRate:0.0} rad/s  stab {_playerController.Stability:0.00}\n" +
                $"seed {_playerController.StoneSeed}";
            ShadowLabel(new Rect(width - 420f * s, 16f * s, 400f * s, 140f * s), debug, _debugStyle, new Color(1f, 1f, 1f, 0.8f));
        }
    }

    /// <summary>
    /// 선택한 던질 거리(돌 또는 도감에 등록된 물고기)를 업그레이드 배율과 함께 던진다.
    /// _projectileIndex와 진행 상황을 사용하며, _state를 변경한다.
    /// </summary>
    private void Throw()
    {
        FishType fish = SelectedFish();
        ThrowModifiers modifiers = fish != null
            ? fish.ToThrowModifiers(_progress.PowerMultiplier, _progress.SpinMultiplier)
            : ThrowModifiers.ForStone(_progress.PowerMultiplier, _progress.SpinMultiplier);
        _state = State.Flying;
        _playerController.Throw(modifiers);
    }

    /// <summary>
    /// 새 던질 거리를 만들고 돌과 카메라를 던지기 전 상태로 되돌린다.
    /// 입력값은 없으며, 물고기, 던질 거리 메시, 돌 상태, 카메라, _state를 변경한다.
    /// </summary>
    private void Restart()
    {
        _fishSpawner.Clear();
        ApplyProjectileShape();
        _playerController.ResetToStart();
        _cameraController.SnapBehindTarget();
        _popupTime = -10f;
        _perfectCount = 0;
        _goodCount = 0;
        _missCount = 0;
        _state = State.Ready;
        LockCursor(true);
    }

    /// <summary>
    /// 돌과 도감에 등록된 물고기 사이에서 던질 거리를 바꾼다. 등록되지 않은 종류는 건너뛴다.
    /// direction(-1 또는 1)을 사용하며, _projectileIndex와 던질 거리 메시를 변경한다.
    /// </summary>
    private void CycleProjectile(int direction)
    {
        int optionCount = _fishSpawner.FishTypes.Count + 1;
        int option = _projectileIndex + 1;
        for (int i = 0; i < optionCount; i++)
        {
            option = (option + direction + optionCount) % optionCount;
            if (option == 0 || _progress.IsFishRegistered(_fishSpawner.FishTypes[option - 1])) break;
        }

        int index = option - 1;
        if (index == _projectileIndex) return;
        _projectileIndex = index;
        ApplyProjectileShape();
    }

    /// <summary>
    /// 선택한 던질 거리에 맞게 돌 또는 물고기 메시를 만든다. 물고기는 던지기 전부터 파닥이게 한다.
    /// _projectileIndex를 사용하며, 던질 거리 메시와 파닥임 상태를 변경한다.
    /// </summary>
    private void ApplyProjectileShape()
    {
        FishType fish = SelectedFish();
        if (fish != null)
        {
            _stoneGenerator.GenerateFish(_fishSpawner.FishPrefabs[_projectileIndex]);
            _stoneGenerator.SetFlopping(true);
        }
        else
        {
            _stoneGenerator.Generate();
        }
    }

    /// <summary>
    /// 현재 선택된 물고기 종류를 구한다.
    /// _projectileIndex를 사용하며, 돌이면 null을 반환한다.
    /// </summary>
    private FishType SelectedFish()
    {
        return _projectileIndex == STONE_INDEX ? null : _fishSpawner.FishTypes[_projectileIndex];
    }

    /// <summary>
    /// 튕김 횟수 문구를 띄운다. 판정이나 포획 문구가 막 떴으면 덮어쓰지 않는다.
    /// count와 judge를 사용하며, 팝업 상태를 변경한다.
    /// </summary>
    private void HandleSkip(int count, SkipJudge judge)
    {
        if (Time.unscaledTime - _popupTime > 0.4f)
        {
            ShowPopup($"SKIP {count}", Color.white);
        }
    }

    /// <summary>
    /// SPACE 판정 결과를 표시하고, 성공하면 선택한 프리팹의 애니메이션 또는 레이저를 재생한다.
    /// judge와 timingError(초, 빠르면 음수)를 사용하며, 팝업, 판정 횟수와 프리팹 연출 상태를 변경한다.
    /// </summary>
    private void HandleJudge(SkipJudge judge, float timingError)
    {
        if (judge == SkipJudge.Perfect || judge == SkipJudge.Good)
        {
            BallController ballController = _stoneGenerator.GetComponentInChildren<BallController>();
            if (ballController != null)
            {
                ballController.PlayAttackSequence();
            }
            HopakJumpAnimation hopakAnimation = _stoneGenerator.GetComponentInChildren<HopakJumpAnimation>();
            if (hopakAnimation != null)
            {
                hopakAnimation.PlayJumpSegment();
            }
            AlkagiLaser laser = _stoneGenerator.GetComponentInChildren<AlkagiLaser>();
            if (laser != null)
            {
                laser.Fire();
            }
        }

        string direction = timingError < 0f ? "EARLY" : "LATE";
        switch (judge)
        {
            case SkipJudge.Perfect:
                _perfectCount++;
                ShowPopup("PERFECT!", JudgeColor(judge));
                break;
            case SkipJudge.Good:
                _goodCount++;
                ShowPopup($"GOOD  {direction}", JudgeColor(judge));
                break;
            case SkipJudge.Miss:
                _missCount++;
                ShowPopup($"MISS  TOO {direction}", JudgeColor(judge));
                break;
        }
    }

    /// <summary>
    /// 처음 물에 닿는 순간 던진 물고기의 파닥임을 멈춘다.
    /// point와 speed는 쓰지 않으며, 던질 거리의 파닥임을 변경한다.
    /// </summary>
    private void HandleWaterContact(Vector3 point, float speed)
    {
        _stoneGenerator.SetFlopping(false);
    }

    /// <summary>
    /// 게임오버 상점에서 게임패드 버튼과 SPACE를 처리한다. X는 던지는 힘, B는 스핀 업그레이드, SPACE와 A는 다시하기다.
    /// gamepad와 jumpPressed를 사용하며, 업그레이드 단계나 게임 상태를 변경한다.
    /// </summary>
    private void UpdateShopInput(Gamepad gamepad, bool jumpPressed)
    {
        if (gamepad != null && gamepad.buttonWest.wasPressedThisFrame)
        {
            _progress.TryUpgrade(UpgradeType.Power);
        }
        if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
        {
            _progress.TryUpgrade(UpgradeType.Spin);
        }
        if (jumpPressed && Time.unscaledTime - _gameOverTime >= RETRY_INPUT_DELAY)
        {
            Restart();
        }
    }

    /// <summary>
    /// 물고기를 잡았을 때 번 돈과 이름을 띄운다.
    /// fish를 사용하며, 팝업 상태를 변경한다.
    /// </summary>
    private void HandleFishCaught(FishType fish)
    {
        ShowPopup($"+{fish.Value} G   {fish.DisplayName}", new Color(1f, 0.85f, 0.3f));
    }

    /// <summary>
    /// 게임오버 상태로 바꾸고 최고 기록을 저장한 뒤, 상점 버튼을 누를 수 있게 커서를 푼다.
    /// reason과 현재 튕김 횟수, 거리를 사용하며, _state와 최고 기록, PlayerPrefs, 커서를 변경한다.
    /// </summary>
    private void HandleGameOver(GameOverReason reason)
    {
        _state = State.GameOver;
        _gameOverReason = reason;
        _gameOverTime = Time.unscaledTime;

        if (_playerController.SkipCount > _bestSkips)
        {
            _bestSkips = _playerController.SkipCount;
            PlayerPrefs.SetInt(BEST_SKIPS_KEY, _bestSkips);
        }
        if (_playerController.Distance > _bestDistance)
        {
            _bestDistance = _playerController.Distance;
            PlayerPrefs.SetFloat(BEST_DISTANCE_KEY, _bestDistance);
        }
        PlayerPrefs.Save();
        LockCursor(false);
    }

    /// <summary>
    /// 던지기 전 안내와 지금 던질 거리를 그린다.
    /// 화면 크기와 scale을 사용하며, 화면에 라벨을 그린다.
    /// </summary>
    private void DrawReady(float width, float height, float scale)
    {
        ShadowLabel(new Rect(0f, height * 0.18f, width, 80f * scale), "SPACE TO THROW", _centerStyle, Color.white);
        ShadowLabel(new Rect(0f, height * 0.18f + 80f * scale, width, 40f * scale),
            "ARROWS tilt    A/D curve    SPACE when it hits the water    R new stone", _centerSubStyle, Color.white);
        ShadowLabel(new Rect(0f, height * 0.18f + 112f * scale, width, 40f * scale),
            "PAD   A throw/skip    R-stick tilt    L-stick/triggers curve    Y new stone", _centerSubStyle, new Color(1f, 1f, 1f, 0.7f));

        FishType fish = SelectedFish();
        string projectile = fish != null ? fish.DisplayName : "STONE";
        ShadowLabel(new Rect(0f, height * _spaceHintHeight, width, 40f * scale),
            $"<  Q/LB   THROW: {projectile}   E/RB  >", _centerSubStyle, new Color(1f, 0.92f, 0.7f));
    }

    /// <summary>
    /// 게임오버 결과와 업그레이드 상점, 다시하기 버튼을 그린다.
    /// 화면 크기, scale, 진행 상황을 사용하며, 버튼을 누르면 업그레이드하거나 재시작한다.
    /// </summary>
    private void DrawShop(float width, float height, float scale)
    {
        ShadowLabel(new Rect(0f, height * 0.12f, width, 80f * scale), "GAME OVER", _centerStyle, Color.white);
        string result = $"{ReasonText(_gameOverReason)}    {_playerController.SkipCount} skips / {_playerController.Distance:0.0} m";
        ShadowLabel(new Rect(0f, height * 0.12f + 80f * scale, width, 40f * scale), result, _centerSubStyle, Color.white);
        string timing = $"PERFECT {_perfectCount}    GOOD {_goodCount}    MISS {_missCount}";
        ShadowLabel(new Rect(0f, height * 0.12f + 115f * scale, width, 40f * scale), timing, _centerSubStyle, new Color(1f, 0.92f, 0.7f));

        // 화면 가운데는 돌 자리라서 버튼은 아래쪽에 둔다.
        float buttonWidth = 620f * scale;
        float buttonHeight = 60f * scale;
        float left = (width - buttonWidth) * 0.5f;
        float top = height * 0.6f;
        float gap = 10f * scale;
        DrawUpgradeButton(new Rect(left, top, buttonWidth, buttonHeight), UpgradeType.Power, "[X] THROW POWER");
        DrawUpgradeButton(new Rect(left, top + buttonHeight + gap, buttonWidth, buttonHeight), UpgradeType.Spin, "[B] SPIN");

        if (GUI.Button(new Rect(left, top + (buttonHeight + gap) * 2f + gap, buttonWidth, buttonHeight), "RETRY   (SPACE / A / R)", _buttonStyle))
        {
            Restart();
        }
    }

    /// <summary>
    /// 판정별 표시 색을 구한다.
    /// judge를 사용하며, PERFECT 금색, GOOD 초록, MISS 회색을 반환한다.
    /// </summary>
    private static Color JudgeColor(SkipJudge judge)
    {
        if (judge == SkipJudge.Perfect) return new Color(1f, 0.85f, 0.2f);
        if (judge == SkipJudge.Good) return new Color(0.4f, 1f, 0.5f);
        return new Color(0.75f, 0.75f, 0.75f);
    }

    /// <summary>
    /// 업그레이드 버튼 하나를 그린다. 최대 단계이거나 돈이 모자라면 누를 수 없다.
    /// rect, type, label을 사용하며, 누르면 업그레이드를 시도한다.
    /// </summary>
    private void DrawUpgradeButton(Rect rect, UpgradeType type, string label)
    {
        int level = _progress.GetLevel(type);
        int cost = _progress.GetNextCost(type);
        string text = cost < 0
            ? $"{label}   Lv {level}/{_progress.MaxLevel}   MAX"
            : $"{label}   Lv {level}/{_progress.MaxLevel}   ->   {cost} G";

        GUI.enabled = cost >= 0 && _progress.Money >= cost;
        if (GUI.Button(rect, text, _buttonStyle))
        {
            _progress.TryUpgrade(type);
        }
        GUI.enabled = true;
    }

    /// <summary>
    /// 화면 위쪽에 잠깐 떠오르는 문구를 설정한다.
    /// text와 color를 사용하며, _popupText, _popupColor, _popupTime을 변경한다.
    /// </summary>
    private void ShowPopup(string text, Color color)
    {
        _popupText = text;
        _popupColor = color;
        _popupTime = Time.unscaledTime;
    }

    /// <summary>
    /// 마우스 커서를 잠그거나 푼다.
    /// locked를 사용하며, Cursor.lockState와 Cursor.visible을 변경한다.
    /// </summary>
    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>
    /// 게임오버 이유를 화면 표시용 문구로 바꾼다.
    /// reason을 사용하며, 표시 문자열을 반환한다.
    /// </summary>
    private static string ReasonText(GameOverReason reason)
    {
        switch (reason)
        {
            case GameOverReason.Sunk: return "Sank";
            case GameOverReason.Stopped: return "Stopped";
            case GameOverReason.HitGround: return "Hit the ground";
            case GameOverReason.OutOfBounds: return "Out of bounds";
            default: return reason.ToString();
        }
    }

    /// <summary>
    /// 화면 높이에 맞춘 HUD 글자 스타일을 만든다. 해상도가 그대로면 다시 만들지 않는다.
    /// Screen.height를 사용하며, GUIStyle 필드와 _styleScale을 변경한다.
    /// </summary>
    private void EnsureStyles()
    {
        float scale = Mathf.Max(0.5f, Screen.height / 1080f);
        if (_bigStyle != null && Mathf.Approximately(scale, _styleScale)) return;

        _styleScale = scale;
        _bigStyle = MakeStyle(48, TextAnchor.UpperLeft, scale);
        _mediumStyle = MakeStyle(32, TextAnchor.UpperLeft, scale);
        _smallStyle = MakeStyle(22, TextAnchor.UpperLeft, scale);
        _centerStyle = MakeStyle(56, TextAnchor.MiddleCenter, scale);
        _centerSubStyle = MakeStyle(22, TextAnchor.UpperCenter, scale);
        _popupStyle = MakeStyle(52, TextAnchor.MiddleCenter, scale);
        _debugStyle = MakeStyle(18, TextAnchor.UpperRight, scale);
        _debugStyle.fontStyle = FontStyle.Normal;
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(22 * scale),
            fontStyle = FontStyle.Bold,
        };
    }

    /// <summary>
    /// 굵은 흰색 라벨 스타일을 만든다.
    /// size, anchor, scale을 사용하며, 새 GUIStyle을 반환한다.
    /// </summary>
    private static GUIStyle MakeStyle(int size, TextAnchor anchor, float scale)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * scale),
            alignment = anchor,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
        };
        style.normal.textColor = Color.white;
        return style;
    }

    /// <summary>
    /// 그림자를 깔고 라벨을 그린다.
    /// rect, text, style, color를 사용하며, 화면에 라벨을 그린다.
    /// </summary>
    private static void ShadowLabel(Rect rect, string text, GUIStyle style, Color color)
    {
        Color original = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.6f * color.a);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = original;
    }
}
