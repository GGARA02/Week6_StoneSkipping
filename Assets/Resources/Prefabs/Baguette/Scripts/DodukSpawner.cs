using System;

using UnityEngine;
using UnityEngine.Serialization;

// 도둑 스포너 위치에서 DodukSpread 도둑 오브젝트를 생성하고 정면(Z축+) 방향으로 발사한다.
public class DodukSpawner : MonoBehaviour
{
    [Header("프리팹")]
    [FormerlySerializedAs("_dodukl")]
    [SerializeField]
    private GameObject _dodukPrefab;

    [Header("발사 설정")]
    [Tooltip("창문 밖으로 튀어나가는 기본 발사 속도")]
    [SerializeField]
    private float _launchSpeed = 15f;
    [Tooltip("상향으로 솟구치는 기본 속도")]
    [SerializeField]
    private float _upwardSpeed = 6f;

    [Header("상태")]
    private SkipEffect _skipEffect;
    private float _waterY;
    private bool _isInitialized;

    void Start()
    {
        InitializeReferences();
    }

    /// <summary>
    /// 씬에서 물보라 이펙트와 수면 높이를 탐색하여 초기화한다.
    /// 씬의 SkipEffect와 Water 콜라이더를 사용하며, _skipEffect와 _waterY를 설정한다.
    /// </summary>
    private void InitializeReferences()
    {
        if (_isInitialized) return;

        _skipEffect = FindFirstObjectByType<SkipEffect>();
        GameObject water = GameObject.FindWithTag("Water");
        if (water != null && water.TryGetComponent<Collider>(out Collider waterCollider))
        {
            _waterY = waterCollider.bounds.max.y;
        }
        else
        {
            _waterY = 0f;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// 스포너의 위치와 전방 방향을 기준으로 DodukSpread 도둑을 생성하여 발사한다.
    /// _dodukPrefab, _launchSpeed, _upwardSpeed를 사용하며, DodukSpread를 발사한다.
    /// </summary>
    public void SpawnDoDuk()
    {
        if (_dodukPrefab == null) return;

        if (!_isInitialized)
        {
            InitializeReferences();
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;
        GameObject dodukObj = Instantiate(_dodukPrefab, spawnPosition, spawnRotation);

        DodukSpread doduk = dodukObj.GetComponent<DodukSpread>();
        if (doduk == null)
        {
            doduk = dodukObj.AddComponent<DodukSpread>();
        }

        // 스포너 전방 방향과 상향 속도를 조합하여 발사
        float speed = _launchSpeed * UnityEngine.Random.Range(0.85f, 1.15f);
        float upward = _upwardSpeed * UnityEngine.Random.Range(0.85f, 1.15f);
        Vector3 launchVelocity = (transform.forward * speed) + (Vector3.up * upward);

        doduk.Launch(launchVelocity, _waterY, _skipEffect);
    }
}
