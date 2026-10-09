using System.Collections;
using UnityEngine;

public class StreamController : MonoBehaviour
{
    [SerializeField] private float _upwardForce = 20f;
    [SerializeField] private float _heightCorrection = 10f;
    [SerializeField] private float _correctionDistance = 5f;

    private Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Rigidbody rb = other.attachedRigidbody;
        float targetTopY = _collider.bounds.max.y;
        float playerTopY = other.transform.position.y;

        float heightDifference = targetTopY - playerTopY;

        float additionalForce = 0f;

        if (heightDifference > 0f)
        {
            float t = Mathf.Clamp01(
                heightDifference / _correctionDistance
            );

            // Ease-Out Sine
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);

            additionalForce = eased * _heightCorrection;
        }

        rb.AddForce(
            Vector3.up * (_upwardForce + additionalForce),
            ForceMode.Acceleration
        );
    }
}
