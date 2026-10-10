using UnityEngine;

// 하늘 ALKAGI가 환경 구름층에 주는 영향. ALKAGI 주변에 구름을 모으고, 눈 충전 빛과 레이저를 따라 한 줄로 갈라지는 구름을 구름층 셰이더에 넘긴다.
// 비 덮임은 기존 날씨가 그대로 정하며, 맑은 날에도 주변 구름이 보이도록 구름층을 켜 두고 획득 후에는 주변 구름을 걷는다.
public class SkyAlkagiClouds : MonoBehaviour
{
    private static readonly int FOCUS_WEIGHT_ID = Shader.PropertyToID("_FocusWeight");
    private static readonly int FOCUS_DIR_ID = Shader.PropertyToID("_FocusDir");
    private static readonly int GLOW_ID = Shader.PropertyToID("_Glow");
    private static readonly int CUT_AMOUNT_ID = Shader.PropertyToID("_CutAmount");
    private static readonly int CUT_GLOW_ID = Shader.PropertyToID("_CutGlow");

    [Header("참조")]
    private EnvironmentController _environment;

    [Header("갈라짐")]
    [Tooltip("레이저 발사 후 구름이 갈라지기 시작할 때까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float _startDelay = 0f;
    [Tooltip("갈라진 줄이 다 벌어지기까지 걸리는 시간(초). 경계의 빛도 이 시간 동안 옅어져 사라진다")]
    [SerializeField, Min(0.01f)] private float _openDuration = 2.5f;
    [Tooltip("벌어지는 시간(0~1)에 따른 벌어진 정도(0~1). 앞쪽이 가파를수록 선 가까이는 빠르게, 먼 곳은 느리게 걷힌다")]
    [SerializeField] private AnimationCurve _openCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 3f), new Keyframe(1f, 1f, 0f, 0f));
    [Tooltip("다 벌어졌을 때의 너비 배율. 구름층 Cut Width에 곱한다")]
    [SerializeField, Min(1f)] private float _openScale = 12f;
    [Tooltip("다 벌어진 구름이 다시 모이는 시간. 대기, 벌어짐, 모임 시간의 합이 다음 발사까지의 간격보다 짧아야 한다")]
    [SerializeField, Min(0.01f)] private float _healDuration = 4.5f;
    private float _cutTime = Mathf.Infinity;

    [Header("사라짐")]
    [Tooltip("획득 후 ALKAGI 주변 구름이 걷히는 시간")]
    [SerializeField, Min(0.01f)] private float _fadeDuration = 2f;
    private float _focusWeight;
    private bool _fading;

    void Awake()
    {
        // 던질 때 바뀌는 날씨와 같은 구름층을 쓰도록 씬의 환경 컨트롤러를 찾아서 쓴다.
        // SkyAlkagi가 Start에서 등장 여부를 정하며 Show나 Hide를 부르므로 Awake에서 미리 찾는다.
        _environment = FindFirstObjectByType<EnvironmentController>();
    }

    void Update()
    {
        _cutTime += Time.deltaTime;
        if (!_fading || _focusWeight <= 0f) return;
        _focusWeight = Mathf.MoveTowards(_focusWeight, 0f, Time.deltaTime / _fadeDuration);
        // 주변 구름이 다 걷히면 구름층 표시를 날씨에 맡긴다.
        if (_focusWeight <= 0f) _environment.SetCloudFocus(false);
    }

    /// <summary>
    /// ALKAGI 방향과 충전 정도, 갈라진 줄의 너비 배율과 경계 빛 세기를 환경 구름층 머티리얼에 넘긴다.
    /// cameraPosition, giantPosition과 charge(0~1)를 사용하며 구름층 머티리얼 속성을 변경한다.
    /// </summary>
    public void UpdateView(Vector3 cameraPosition, Vector3 giantPosition, float charge)
    {
        Material material = _environment.CloudMaterial;
        material.SetFloat(FOCUS_WEIGHT_ID, _focusWeight);
        material.SetVector(FOCUS_DIR_ID, (giantPosition - cameraPosition).normalized);
        material.SetFloat(GLOW_ID, charge);
        material.SetFloat(CUT_AMOUNT_ID, CutWidthScale());
        // 경계 빛은 갈라질 때 가장 밝고 다 벌어질 때 사라진다. 기다리는 동안에는 켜지 않는다.
        material.SetFloat(CUT_GLOW_ID, _cutTime < 0f ? 0f : 1f - Mathf.Clamp01(_cutTime / _openDuration));
    }

    /// <summary>
    /// 갈라지기 시작한 뒤 흐른 시간으로 갈라진 줄의 너비 배율을 구한다. _openCurve를 따라 벌어진 뒤 _healDuration 동안 줄어 다시 모인다.
    /// _cutTime, 벌어짐 커브와 시간, 모임 시간을 사용하며 0(갈라짐 없음) 이상의 배율을 반환한다.
    /// </summary>
    private float CutWidthScale()
    {
        if (_cutTime < 0f) return 0f;
        float open = Mathf.Lerp(1f, _openScale, _openCurve.Evaluate(Mathf.Clamp01(_cutTime / _openDuration)));
        if (_cutTime < _openDuration) return open;
        return open * (1f - Mathf.SmoothStep(0f, 1f, (_cutTime - _openDuration) / _healDuration));
    }

    /// <summary>
    /// 레이저 발사에 맞춰 _startDelay 뒤에 돔을 가로지르는 한 줄로 구름을 가르기 시작하게 한다.
    /// _startDelay를 사용하며 갈라짐 시작 기준 시간을 음수부터 다시 센다.
    /// </summary>
    public void BeginCut()
    {
        // 기다리는 동안은 시간이 음수라 셰이더에서 어느 지점도 갈라지지 않는다.
        _cutTime = -_startDelay;
    }

    /// <summary>
    /// 재시작 시 갈라진 구름을 기다리지 않고 바로 다 덮인 상태로 되돌린다.
    /// 입력값 없이 발사 후 흐른 시간을 갈라짐이 없는 상태로 바꾼다.
    /// </summary>
    public void ResetCut()
    {
        _cutTime = Mathf.Infinity;
    }

    /// <summary>
    /// 하늘 ALKAGI가 나타난 판에서 주변 구름을 바로 모으고 맑은 날에도 구름층을 켠다.
    /// 입력값 없이 주변 구름 비중과 사라짐 상태, 환경의 구름층 유지 여부를 변경한다.
    /// </summary>
    public void Show()
    {
        _fading = false;
        _focusWeight = 1f;
        _environment.SetCloudFocus(true);
    }

    /// <summary>
    /// 하늘 ALKAGI가 없는 판에서 주변 구름을 바로 없애고 구름층 표시를 날씨에 맡긴다.
    /// 입력값 없이 주변 구름 비중과 사라짐 상태, 환경의 구름층 유지 여부를 변경한다.
    /// </summary>
    public void Hide()
    {
        _fading = false;
        _focusWeight = 0f;
        _environment.SetCloudFocus(false);
    }

    /// <summary>
    /// 획득 후 ALKAGI 주변 구름이 _fadeDuration 동안 걷히게 한다.
    /// 입력값 없이 사라짐 진행 상태를 변경한다.
    /// </summary>
    public void FadeOut()
    {
        _fading = true;
    }
}
