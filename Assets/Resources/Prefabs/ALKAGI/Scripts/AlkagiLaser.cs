using UnityEngine;

public class AlkagiLaser : MonoBehaviour
{
    [Header("레이저")]
    [SerializeField] private Transform _eye;
    [SerializeField] private GameObject _beamPrefab;
    [SerializeField] private float _duration = 0.35f;

    [Header("상태")]
    private GameObject _beam;
    private float _remainingTime;

    void Update()
    {
        if (_remainingTime <= 0f) return;

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
        }

        _beam.SetActive(true);
        _remainingTime = _duration;
    }
}
