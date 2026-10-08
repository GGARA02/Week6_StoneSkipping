using UnityEngine;

// Week4 화살이 SPACE 판정에 성공했을 때 나오는 불씨 파티클. 터져 나온 뒤 잠시 기다렸다가 대상에게 빨려 들어간다.
// Week4ParticleAttractor를 참고했으며, ArrowController 없이 대상만 받아 동작한다.
[RequireComponent(typeof(ParticleSystem))]
public class Week4ArrowGainEffect : MonoBehaviour
{
    [Header("흡수")]
    [Tooltip("터져 나온 뒤 빨려 들어가기 시작할 때까지 기다리는 시간(초)")]
    [SerializeField]
    private float _delayTime = 0.4f;
    [Tooltip("빨려 들어가는 속도")]
    [SerializeField]
    private float _speed = 100f;
    [Tooltip("대상에 이 거리보다 가까워지면 흡수된 것으로 보고 없앤다")]
    [SerializeField]
    private float _absorbDistance = 0.3f;
    private ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] _particles;
    private Transform _target;
    private float _timer;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer <= _delayTime) return;

        // 다시 고르거나 재시작하면 대상 화살이 먼저 사라질 수 있다.
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        int count = _particleSystem.GetParticles(_particles);
        if (count == 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = _target.position;
        float step = _speed * Time.deltaTime;
        for (int i = 0; i < count; i++)
        {
            _particles[i].position = Vector3.MoveTowards(_particles[i].position, targetPosition, step);
            if (Vector3.Distance(_particles[i].position, targetPosition) < _absorbDistance)
            {
                _particles[i].remainingLifetime = 0f;
            }
        }
        _particleSystem.SetParticles(_particles, count);
    }

    /// <summary>
    /// 파티클이 빨려 들어갈 대상을 정한다.
    /// target을 사용하며, _target을 변경한다.
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
