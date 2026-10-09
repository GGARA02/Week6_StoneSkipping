using System;

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public class AideCharacterAnimator : MonoBehaviour
{
    [Header("캐릭터 움직임")]
    [SerializeField] private float _bobAmplitude = 4f;
    [SerializeField, Min(0.1f)] private float _bobPeriod = 3.2f;
    [SerializeField] private RectTransform _handRect;
    private RawImage _characterImage;
    private RectTransform _characterRect;
    private Vector2 _characterBasePosition;
    private Vector2 _handBasePosition;
    private float _bobElapsed;

    [Header("표정 이미지")]
    [SerializeField] private Texture2D[] _expressions = Array.Empty<Texture2D>();
    private int _expressionIndex;

    [Header("임시 표정 미리보기")]
    [SerializeField] private bool _previewExpressions = true;
    [SerializeField, Min(0.1f)] private float _previewInterval = 3f;
    private float _previewElapsed;

    [Header("접속 상태 표시")]
    [SerializeField] private CanvasGroup _onlineIndicator;
    [SerializeField] private CanvasGroup _offlineIndicator;
    [SerializeField] private bool _isOnline = true;
    [SerializeField, Min(0.1f)] private float _statusBlinkPeriod = 1.4f;
    [SerializeField, Range(0f, 1f)] private float _statusMinAlpha = 0.35f;
    private float _statusElapsed;

    void Awake()
    {
        _characterImage = GetComponent<RawImage>();
        _characterRect = _characterImage.rectTransform;
        _characterBasePosition = _characterRect.anchoredPosition;
        if (_handRect != null)
            _handBasePosition = _handRect.anchoredPosition;

        SetExpression(0);
        SetConnectionStatus(_isOnline);
    }

    void OnEnable()
    {
        _bobElapsed = 0f;
        _previewElapsed = 0f;
        _statusElapsed = 0f;
    }

    void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        AnimateBobbing(deltaTime);
        PreviewExpressions(deltaTime);
        AnimateConnectionIndicator(deltaTime);
    }

    void OnDisable()
    {
        _characterRect.anchoredPosition = _characterBasePosition;
        if (_handRect != null)
            _handRect.anchoredPosition = _handBasePosition;
        _onlineIndicator.alpha = 1f;
        _offlineIndicator.alpha = 1f;
    }

    /// <summary>
    /// 경과 시간 deltaTime으로 활성 접속 표시의 점과 문구를 함께 깜빡이게 한다.
    /// 설정된 주기와 최소 투명도를 사용해 ONLINE 또는 OFFLINE 그룹의 alpha를 갱신한다.
    /// </summary>
    private void AnimateConnectionIndicator(float deltaTime)
    {
        float period = Mathf.Max(0.1f, _statusBlinkPeriod);
        _statusElapsed = (_statusElapsed + deltaTime) % period;
        float brightness = (Mathf.Cos(_statusElapsed / period * Mathf.PI * 2f) + 1f) * 0.5f;
        CanvasGroup indicator = _isOnline ? _onlineIndicator : _offlineIndicator;
        indicator.alpha = Mathf.Lerp(_statusMinAlpha, 1f, brightness);
    }

    /// <summary>
    /// isOnline 입력에 따라 초록색 ONLINE 또는 빨간색 OFFLINE 표시로 전환한다.
    /// 현재 접속 상태, 표시 그룹의 활성 상태와 깜빡임 타이머를 갱신한다.
    /// </summary>
    public void SetConnectionStatus(bool isOnline)
    {
        _isOnline = isOnline;
        _statusElapsed = 0f;
        _onlineIndicator.gameObject.SetActive(isOnline);
        _offlineIndicator.gameObject.SetActive(!isOnline);
        _onlineIndicator.alpha = 1f;
        _offlineIndicator.alpha = 1f;
    }

    /// <summary>
    /// 경과 시간 deltaTime으로 캐릭터와 연결된 Hand를 부드럽게 위아래로 이동한다.
    /// 설정된 진폭과 주기를 사용해 기준 위치에 수직 변위를 더한다.
    /// </summary>
    private void AnimateBobbing(float deltaTime)
    {
        float period = Mathf.Max(0.1f, _bobPeriod);
        _bobElapsed = (_bobElapsed + deltaTime) % period;
        float offset = Mathf.Sin(_bobElapsed / period * Mathf.PI * 2f) * _bobAmplitude;
        Vector2 movement = Vector2.up * offset;
        _characterRect.anchoredPosition = _characterBasePosition + movement;
        if (_handRect != null)
            _handRect.anchoredPosition = _handBasePosition + movement;
    }

    /// <summary>
    /// 경과 시간 deltaTime을 누적해 임시 미리보기 간격마다 다음 표정으로 변경한다.
    /// 미리보기 설정과 이미지 배열을 사용하며 마지막 표정 다음에는 첫 표정으로 돌아간다.
    /// </summary>
    private void PreviewExpressions(float deltaTime)
    {
        if (!_previewExpressions || _expressions.Length == 0)
            return;

        _previewElapsed += deltaTime;
        float interval = Mathf.Max(0.1f, _previewInterval);
        if (_previewElapsed < interval)
            return;

        SetExpression((_expressionIndex + 1) % _expressions.Length);
    }

    /// <summary>
    /// enabled 입력으로 임시 표정 순환을 켜거나 끈다.
    /// 현재 표정은 유지하고 미리보기 타이머를 초기화한다.
    /// </summary>
    public void SetExpressionPreview(bool enabled)
    {
        _previewExpressions = enabled;
        _previewElapsed = 0f;
    }

    /// <summary>
    /// 이미지 이름 expressionName과 일치하는 표정으로 캐릭터 이미지를 변경한다.
    /// 파일 확장자 없는 이름을 대소문자 구분 없이 비교하며 변경 성공 여부를 반환한다.
    /// </summary>
    public bool SetExpression(string expressionName)
    {
        for (int index = 0; index < _expressions.Length; index++)
        {
            Texture2D expression = _expressions[index];
            if (expression != null && string.Equals(expression.name, expressionName, StringComparison.OrdinalIgnoreCase))
                return SetExpression(index);
        }

        return false;
    }

    /// <summary>
    /// 이미지 배열의 expressionIndex 위치에 있는 표정으로 캐릭터 이미지를 변경한다.
    /// 유효한 이미지이면 현재 표정 인덱스와 미리보기 타이머를 갱신하고 true를 반환한다.
    /// </summary>
    public bool SetExpression(int expressionIndex)
    {
        if (expressionIndex < 0 || expressionIndex >= _expressions.Length || _expressions[expressionIndex] == null)
            return false;

        _characterImage.texture = _expressions[expressionIndex];
        _expressionIndex = expressionIndex;
        _previewElapsed = 0f;
        return true;
    }
}
