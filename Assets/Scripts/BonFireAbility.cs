using UnityEngine;

// 모닥불의 특수 동작. 던졌을 때 SPACE 판정에 성공하면 일정 시간 불이 붙어 불꽃 이펙트와 불빛이 켜진다.
// 다시 성공하면 다시 불이 붙고 남은 시간이 처음부터 다시 시작된다.
public class BonFireAbility : FishAbility
{
    [Header("불")]
    [Tooltip("불이 붙을 때 재생하는 불꽃 파티클")]
    [SerializeField]
    private ParticleSystem[] _fireParticles;
    [Tooltip("불이 붙을 때 켜는 불빛 오브젝트. 프리팹에서는 꺼 둔다")]
    [SerializeField]
    private GameObject[] _fireLights;
    [Tooltip("한 번 불이 붙었을 때 켜져 있는 시간(초)")]
    [SerializeField]
    private float _burnDuration = 1f;

    [Header("상태")]
    private float _burnTimeLeft;

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
    /// 불꽃 파티클과 불빛을 함께 켜거나 끈다.
    /// lit, _fireParticles, _fireLights를 사용하며, 파티클 재생 상태와 불빛 활성 상태를 변경한다.
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
        foreach (GameObject fireLight in _fireLights)
        {
            fireLight.SetActive(lit);
        }
    }
}
