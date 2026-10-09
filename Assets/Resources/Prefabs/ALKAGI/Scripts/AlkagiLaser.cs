using System.Collections.Generic;

using UnityEngine;

public class AlkagiLaser : MonoBehaviour
{
    [Header("레이저")]
    [SerializeField] private Transform _eye;
    [SerializeField] private GameObject _beamPrefab;
    [SerializeField] private float _duration = 0.35f;

    [Header("상태")]
    private GameObject _beam;
    private CapsuleCollider _beamCollider;
    private float _remainingTime;
    private readonly HashSet<CuttableWall> _cutWalls = new HashSet<CuttableWall>();

    void Update()
    {
        if (_remainingTime <= 0f) return;

        CutWalls();
        _remainingTime -= Time.deltaTime;
        if (_remainingTime <= 0f)
        {
            _beam.SetActive(false);
        }
    }

    void OnDisable()
    {
        _remainingTime = 0f;
        if (_beam != null) _beam.SetActive(false);
    }

    /// <summary>
    /// 성공한 SPACE 판정에서 눈의 forward 방향으로 레이저를 표시한다.
    /// _eye와 _beamPrefab, _duration을 사용하며, Beam을 재사용하고 표시 시간을 다시 시작한다.
    /// </summary>
    public void Fire()
    {
        _cutWalls.Clear();
        if (_beam == null)
        {
            _beam = Instantiate(_beamPrefab, _eye, false);
            _beam.transform.localPosition = Vector3.zero;
            _beam.transform.localRotation = Quaternion.identity;
            Vector3 scale = _beamPrefab.transform.localScale;
            Vector3 eyeScale = _eye.localScale;
            _beam.transform.localScale = new Vector3(
                scale.x / eyeScale.x, scale.y / eyeScale.y, scale.z / eyeScale.z);
            foreach (Collider collider in _beam.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            _beamCollider = _beam.GetComponentInChildren<CapsuleCollider>();
        }

        _beam.SetActive(true);
        _remainingTime = _duration;
        CutWalls();
    }

    /// <summary>
    /// 표시 중인 빔의 CapsuleCollider 형상과 눈 방향을 사용해 닿은 벽을 평면 절단한다.
    /// 빔의 물리 충돌은 켜지 않고, 빔 진행 방향을 포함하는 절단면으로 Wall 조각을 만든다.
    /// </summary>
    private void CutWalls()
    {
        Transform beamTransform = _beamCollider.transform;
        int axis = _beamCollider.direction;
        Vector3 scale = beamTransform.lossyScale;
        float radius = _beamCollider.radius * Mathf.Max(Mathf.Abs(scale[(axis + 1) % 3]), Mathf.Abs(scale[(axis + 2) % 3]));
        float halfLength = Mathf.Max(0f, _beamCollider.height * Mathf.Abs(scale[axis]) * 0.5f - radius);
        Vector3 direction = axis == 0 ? beamTransform.right : axis == 1 ? beamTransform.up : beamTransform.forward;
        Vector3 center = beamTransform.TransformPoint(_beamCollider.center);
        foreach (Collider collider in Physics.OverlapCapsule(center - direction * halfLength, center + direction * halfLength,
                     radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (collider.TryGetComponent(out CuttableWall wall) && !_cutWalls.Contains(wall))
            {
                Vector3 contact = collider.ClosestPoint(_eye.position + _eye.forward * Vector3.Dot(collider.bounds.center - _eye.position, _eye.forward));
                if (wall.Cut(contact, _eye.up)) _cutWalls.Add(wall);
            }
        }
    }
}
