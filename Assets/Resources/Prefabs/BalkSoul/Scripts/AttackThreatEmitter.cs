using UnityEngine;

public class AttackThreatEmitter : MonoBehaviour
{
    [Header("Threat")]
    [SerializeField] private Transform _threatOrigin;
    [SerializeField] private Transform _directionSource;

    [SerializeField] private float _defaultRange = 2.5f;

    [SerializeField, Range(0f, 360f)]
    private float _defaultAngle = 90f;


    public bool IsThreatActive { get; private set; }

    public float ThreatRange { get; private set; }

    public float ThreatAngle { get; private set; }


    public Vector3 Origin =>
        _threatOrigin != null
            ? _threatOrigin.position
            : transform.position;


    public Vector3 Forward =>
        _directionSource != null
            ? _directionSource.forward
            : transform.forward;


    private void Awake()
    {
        ResetThreatProfile();
    }


    public void SetThreatProfile(
        float range,
        float angle)
    {
        ThreatRange = Mathf.Max(
            0f,
            range
        );

        ThreatAngle = Mathf.Clamp(
            angle,
            0f,
            360f
        );
    }


    public void ResetThreatProfile()
    {
        ThreatRange = _defaultRange;

        ThreatAngle = _defaultAngle;
    }


    // Animation Event
    public void BeginThreat()
    {
        IsThreatActive = true;
    }


    // Animation Event
    public void EndThreat()
    {
        IsThreatActive = false;
    }


    public void CancelThreat()
    {
        IsThreatActive = false;

        ResetThreatProfile();
    }
}
