using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class HopakJumpAnimation : MonoBehaviour
{
    [Header("애니메이션")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private float _playbackSpeed = 0.7f;

    [Header("상태")]
    private PlayableGraph _graph;
    private AnimationClipPlayable _playable;
    private float _time;
    private float _segmentEnd;
    private int _successCount;
    private bool _isPlaying;

    void Awake()
    {
        _graph = PlayableGraph.Create("Hopak Jump");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        _playable = AnimationClipPlayable.Create(_graph, _clip);
        _playable.SetSpeed(0d);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Hopak", _animator);
        output.SetSourcePlayable(_playable);
        _graph.Play();
        _graph.Evaluate(0f);
    }

    void Update()
    {
        if (!_isPlaying) return;

        _time = Mathf.Min(_time + Time.deltaTime * _playbackSpeed, _segmentEnd);
        _playable.SetTime(_time);
        _graph.Evaluate(0f);
        _isPlaying = _time < _segmentEnd;
    }

    void OnDestroy()
    {
        if (_graph.IsValid()) _graph.Destroy();
    }

    /// <summary>
    /// 성공한 점프 순서에 따라 합쳐진 애니메이션의 앞 절반 또는 뒤 절반을 재생한다.
    /// _clip 길이와 성공 횟수를 사용하며, 재생 구간과 현재 자세를 변경한다.
    /// </summary>
    public void PlayJumpSegment()
    {
        _successCount++;
        bool firstHalf = _successCount % 2 == 1;
        _time = firstHalf ? 0f : _clip.length * 0.5f;
        _segmentEnd = firstHalf ? _clip.length * 0.5f : _clip.length;
        _playable.SetTime(_time);
        _graph.Evaluate(0f);
        _isPlaying = true;
    }
}
