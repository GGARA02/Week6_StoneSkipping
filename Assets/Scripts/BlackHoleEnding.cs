using UnityEngine;

public class BlackHoleEnding : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GameFlowManager flowManager;

    private bool _triggered;

    void Awake()
    {
        if (flowManager == null)
        {
            flowManager = FindFirstObjectByType<GameFlowManager>();
        }
    }

    void OnEnable()
    {
        _triggered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;
        _triggered = true;
        flowManager.StartEndingCinema();
        Destroy(gameObject);
    }
}
