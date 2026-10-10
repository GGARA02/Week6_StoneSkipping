using UnityEngine;

// 모닥불의 특수 동작. 던졌을 때 SPACE 판정에 성공하면 일정 시간 불이 붙어 불꽃 이펙트와 불빛이 켜진다.
// 다시 성공하면 다시 불이 붙고 남은 시간이 처음부터 다시 시작된다.
// 불빛 세기는 댐핑을 넣어 켜질 때 서서히 밝아지고 꺼질 때 서서히 어두워진다.
public class BonFireAbility : FishAbility
{
    // 불빛 밝기 비율이 이보다 작아지면 다 꺼진 것으로 보고 불빛 오브젝트를 끈다.
    private const float LIGHT_OFF_LEVEL = 0.005f;

    [Header("불")]
    [Tooltip("불이 붙을 때 재생하는 불꽃 파티클")]
    [SerializeField]
    private ParticleSystem[] _fireParticles;
    [Tooltip("불이 붙을 때 켜는 불빛 오브젝트. 프리팹에서는 꺼 두고, 안의 Light 세기를 켜졌을 때의 밝기로 쓴다")]
    [SerializeField]
    private GameObject[] _fireLights;
    [Tooltip("한 번 불이 붙었을 때 켜져 있는 시간(초)")]
    [SerializeField]
    private float _burnDuration = 2f;
    [Tooltip("불빛 세기가 목표 밝기에 다가가는 시간(초). 클수록 천천히 밝아지고 어두워진다")]
    [SerializeField]
    private float _lightSmoothTime = 0.25f;

    [Header("상태")]
    private float _burnTimeLeft;
    private bool _isLit;
    private Light[] _lights;
    private float[] _lightIntensities;
    // 0은 꺼짐, 1은 프리팹에 설정된 밝기
    private float _lightLevel;
    private float _lightLevelVelocity;

    void Awake()
    {
        _lights = new Light[_fireLights.Length];
        _lightIntensities = new float[_fireLights.Length];
        for (int i = 0; i < _fireLights.Length; i++)
        {
            _lights[i] = _fireLights[i].GetComponent<Light>();
            _lightIntensities[i] = _lights[i].intensity;
        }
        ApplyLightLevel();
    }

    void Update()
    {
        float target = _isLit ? 1f : 0f;
        if (_lightLevel == target) return;

        _lightLevel = Mathf.SmoothDamp(_lightLevel, target, ref _lightLevelVelocity, _lightSmoothTime);
        if (Mathf.Abs(_lightLevel - target) < LIGHT_OFF_LEVEL)
        {
            _lightLevel = target;
            _lightLevelVelocity = 0f;
        }
        ApplyLightLevel();
        if (_lightLevel == 0f)
        {
            SetLightsActive(false);
        }
    }

    /// <summary>
    /// 던진 모닥불이 SPACE 판정에 성공하면 불을 붙이고 켜져 있을 시간을 처음부터 다시 잰다.
    /// _burnDuration을 사용하며, context와 judge는 쓰지 않는다. 불 표시 상태와 _burnTimeLeft를 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        _burnTimeLeft = _burnDuration;
        SetFire(true);
    }

    /// <summary>
    /// 불이 켜져 있으면 남은 시간을 줄이고, 다 되면 불을 끈다.
    /// dt와 _burnTimeLeft를 사용하며, context는 쓰지 않는다. _burnTimeLeft와 불 표시 상태를 변경한다.
    /// </summary>
    public override void UpdateFlight(ThrowContext context, float dt)
    {
        if (_burnTimeLeft <= 0f) return;

        _burnTimeLeft -= dt;
        if (_burnTimeLeft <= 0f)
        {
            SetFire(false);
        }
    }

    /// <summary>
    /// 불꽃 파티클을 켜거나 끄고, 불빛이 서서히 밝아지거나 어두워질 목표를 정한다.
    /// lit, _fireParticles를 사용하며, 파티클 재생 상태와 _isLit, 불빛 오브젝트 활성 상태를 변경한다.
    /// </summary>
    private void SetFire(bool lit)
    {
        foreach (ParticleSystem fireParticle in _fireParticles)
        {
            if (lit)
            {
                fireParticle.Play();
            }
            else
            {
                // 이미 나온 불꽃은 자연스럽게 사라지도록 새로 만드는 것만 멈춘다.
                fireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        _isLit = lit;
        // 꺼질 때는 Update에서 다 어두워진 뒤에 불빛 오브젝트를 끈다.
        if (lit)
        {
            SetLightsActive(true);
        }
    }

    /// <summary>
    /// 현재 밝기 비율을 각 불빛의 세기에 반영한다.
    /// _lightLevel과 _lightIntensities를 사용하며, 각 Light의 intensity를 변경한다.
    /// </summary>
    private void ApplyLightLevel()
    {
        for (int i = 0; i < _lights.Length; i++)
        {
            _lights[i].intensity = _lightIntensities[i] * _lightLevel;
        }
    }

    /// <summary>
    /// 불빛 오브젝트를 모두 켜거나 끈다.
    /// active와 _fireLights를 사용하며, 불빛 오브젝트 활성 상태를 변경한다.
    /// </summary>
    private void SetLightsActive(bool active)
    {
        foreach (GameObject fireLight in _fireLights)
        {
            fireLight.SetActive(active);
        }
    }
}
