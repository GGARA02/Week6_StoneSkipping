using UnityEngine;

public class AlkagiWaterHole : MonoBehaviour
{
    private const float MIN_LENGTH = 0.01f;

    [Header("확장")]
    private Vector3 _startPoint;
    private float _length;

    [Header("축소")]
    [SerializeField, Min(0f)] private float _delay = 2f;
    [SerializeField, Min(0.01f)] private float _shrinkDuration = 1.5f;
    private float _elapsed;
    private float _initialWidth;
    private bool _closing;

    void Update()
    {
        if (!_closing) return;
        _elapsed += Time.deltaTime;
        if (_elapsed < _delay) return;
        Vector3 scale = transform.localScale;
        scale.x = _initialWidth * (1f - Mathf.Clamp01((_elapsed - _delay) / _shrinkDuration));
        transform.localScale = scale;
        if (_elapsed >= _delay + _shrinkDuration) Destroy(gameObject);
    }

    /// <summary>
    /// startPoint를 고정 시작점으로 저장하고 width와 depth로 짧은 물 구멍을 준비한다.
    /// 입력 크기와 시작점을 사용하며 구멍 위치와 스케일 및 축소 상태를 초기화한다.
    /// </summary>
    public void Initialize(Vector3 startPoint, float width, float depth)
    {
        _startPoint = startPoint;
        _length = MIN_LENGTH;
        _closing = false;
        _elapsed = 0f;
        transform.localScale = new Vector3(width, depth, _length);
        transform.position = _startPoint + transform.forward * (_length * 0.5f);
    }

    /// <summary>
    /// point의 진행 방향 거리를 사용해 시작점은 유지하고 앞쪽으로만 구멍을 늘린다.
    /// 기존 길이보다 멀리 도달한 경우 Z 스케일과 중심 위치를 변경한다.
    /// </summary>
    public void ExtendTo(Vector3 point)
    {
        if (_closing) return;
        _length = Mathf.Max(_length, Vector3.Dot(point - _startPoint, transform.forward));
        Vector3 scale = transform.localScale;
        scale.z = _length;
        transform.localScale = scale;
        transform.position = _startPoint + transform.forward * (_length * 0.5f);
    }

    /// <summary>
    /// 발사 종료 후 대기와 너비 축소를 시작한다.
    /// 입력값 없이 현재 X 스케일을 저장하고 종료 상태와 경과 시간을 변경한다.
    /// </summary>
    public void BeginClosing()
    {
        if (_closing) return;
        _initialWidth = transform.localScale.x;
        _elapsed = 0f;
        _closing = true;
    }
}
