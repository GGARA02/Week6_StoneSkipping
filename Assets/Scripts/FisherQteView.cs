using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FisherQteView : MonoBehaviour
{
    [Header("프리팹 UI 참조")]
    [SerializeField] private RectTransform _keyRow;
    [SerializeField] private Text[] _keys;
    [SerializeField] private Slider _timeSlider;
    [SerializeField] private Image _timeFill;
    [SerializeField] private Text _progress;
    [SerializeField] private Text _wins;

    [Header("표시 설정")]
    [SerializeField] private float _keySpacing = 80f;
    [SerializeField] private Color _currentKeyColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _completedKeyColor = new Color(0.45f, 0.5f, 0.6f);
    [SerializeField] private Color _remainingKeyColor = Color.white;
    [SerializeField] private Color _timeColor = new Color(0.1f, 0.9f, 1f);
    [SerializeField] private Color _lowTimeColor = new Color(1f, 0.3f, 0.2f);
    private int _keyCount;

    /// <summary>
    /// sequence와 successes로 프리팹의 키 목록과 성공 기록을 채우고 QTE 화면을 표시한다.
    /// 사용하지 않는 키 칸은 숨기고 시간 슬라이더와 현재 입력 위치를 초기화한다.
    /// </summary>
    public void Show(Key[] sequence, int successes)
    {
        _keyCount = sequence.Length;
        for (int i = 0; i < _keys.Length; i++)
        {
            _keys[i].gameObject.SetActive(i < _keyCount);
            if (i < _keyCount) _keys[i].text = sequence[i].ToString();
        }
        _wins.text = $"QTE wins: {successes}";
        SetProgress(0, 1f);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// inputIndex와 remainingRatio로 현재 키를 중앙에 옮기고 남은 시간과 입력 진행을 표시한다.
    /// 완료된 키의 색을 변경하고 시간이 25퍼센트 이하이면 슬라이더를 경고 색으로 바꾼다.
    /// </summary>
    public void SetProgress(int inputIndex, float remainingRatio)
    {
        _keyRow.anchoredPosition = new Vector2(-inputIndex * _keySpacing, 0f);
        for (int i = 0; i < _keyCount; i++)
            _keys[i].color = i < inputIndex ? _completedKeyColor : i == inputIndex ? _currentKeyColor : _remainingKeyColor;
        _timeSlider.SetValueWithoutNotify(Mathf.Clamp01(remainingRatio));
        _timeFill.color = remainingRatio <= 0.25f ? _lowTimeColor : _timeColor;
        _progress.text = $"{inputIndex} / {_keyCount}";
    }

    /// <summary>
    /// 입력값 없이 QTE Canvas를 비활성화해 화면 표시를 숨긴다.
    /// 기존 키 칸과 참조는 다음 QTE에서 재사용할 수 있도록 유지한다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
