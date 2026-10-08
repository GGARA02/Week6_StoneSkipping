using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    private bool _isAttacking;
    private TrailRenderer[] _trails;

    private void Awake()
    {
        _trails = GetComponentsInChildren<TrailRenderer>();
        UpdateTrailWidths();
    }

    private void LateUpdate()
    {
        UpdateTrailWidths();
    }

    private void UpdateTrailWidths()
    {
        float width = transform.localScale.x * 0.2f;
        foreach (TrailRenderer trail in _trails)
        {
            trail.widthMultiplier = width;
        }
    }

    private void Update()
    {
        if (!_isAttacking && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        _isAttacking = true;

        _animator.CrossFadeInFixedTime("Attack_Light_1", 0.05f, 0, 0f);
        yield return new WaitForSeconds(1f);

        _animator.CrossFadeInFixedTime("Attack_Light_2", 0.05f, 0, 0f);
        yield return new WaitForSeconds(1f);

        _animator.CrossFadeInFixedTime("Attack_Light_3", 0.05f, 0, 0f);
        yield return new WaitForSeconds(1f);

        _isAttacking = false;
    }

    private void OnDisable()
    {
        _isAttacking = false;
    }
}
