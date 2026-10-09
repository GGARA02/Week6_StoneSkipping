using System.Collections;
using UnityEngine;

public class WaterJetController : MonoBehaviour
{
    private enum State
    {
        Prepare,
        Jet,
        Idle
    }

    [SerializeField] private float _prepareDuration = 3f;
    [SerializeField] private float _jetDuration = 3f;
    [SerializeField] private float _idleDuration = 3f;

    [SerializeField] private float _acceleration = 40f;
    [SerializeField] private float _maxUpVelocity = 100f;

    ParticleSystem _smoke;
    ParticleSystem _bubble;
    ParticleSystem _jet;

    private State _state;

    private void Awake()
    {
        _smoke = transform.Find("CaveSSmoke").GetComponent<ParticleSystem>();
        _bubble = transform.Find("Bubble").GetComponent<ParticleSystem>();
        _jet = transform.Find("Jet").GetComponent<ParticleSystem>();

        _smoke.Stop();
        _bubble.Stop();
        _jet.Stop();
    }

    private void Start()
    {
        StartCoroutine(StateRoutine());
    }

    private IEnumerator StateRoutine()
    {
        while (true)
        {
            _state = State.Prepare;
            _bubble.Play();
            yield return new WaitForSeconds(_prepareDuration);

            _state = State.Jet;
            _bubble.Stop();
            _smoke.Stop();
            _jet.Play();

            yield return new WaitForSeconds(
                _jetDuration
            );

            _state = State.Idle;
            _smoke.Play();
            _jet.Stop();
            yield return new WaitForSeconds(_idleDuration);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (_state != State.Jet)
            return;

        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        float currentUpVelocity =
            Vector3.Dot(rb.linearVelocity, Vector3.up);

        if (currentUpVelocity >= _maxUpVelocity)
            return;

        float velocityIncrease =
            _acceleration * Time.fixedDeltaTime;

        velocityIncrease = Mathf.Min(
            velocityIncrease,
            _maxUpVelocity - currentUpVelocity
        );

        rb.linearVelocity +=
            Vector3.up * velocityIncrease;
    }
}
