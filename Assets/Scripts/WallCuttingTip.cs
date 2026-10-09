using UnityEngine;

public sealed class WallCuttingTip : MonoBehaviour
{
    [Header("상태")]
    private PlayerController _player;
    private Vector3 _previousPosition;
    private float _radius;

    /// <summary>
    /// player와 팔 끝의 현재 위치를 사용해 접촉 감지를 준비한다.
    /// 팔 길이에 비례한 감지 반경과 이전 위치를 저장한다.
    /// </summary>
    public void Initialize(PlayerController player)
    {
        _player = player;
        _previousPosition = transform.position;
        _radius = Mathf.Max(Vector3.Distance(transform.position, transform.parent.position) * 0.1f, 0.05f);
    }

    void LateUpdate()
    {
        Vector3 current = transform.position;
        if (!_player.IsThrown || _player.IsGameOver)
        {
            _previousPosition = current;
            return;
        }

        Vector3 movement = current - _previousPosition;
        Vector3 arm = current - transform.parent.position;
        Vector3 normal = Vector3.Cross(movement, arm);
        if (normal.sqrMagnitude < 0.000001f) normal = Vector3.Cross(_player.Velocity, Vector3.up);
        if (normal.sqrMagnitude < 0.000001f) normal = _player.transform.right;

        foreach (Collider collider in Physics.OverlapCapsule(_previousPosition, current, _radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (collider.TryGetComponent(out CuttableWall wall))
            {
                Vector3 contact = collider.ClosestPoint(current);
                wall.Cut(contact, normal);
            }
        }
        _previousPosition = current;
    }
}
