using System;
using System.Collections.Generic;

using UnityEngine;

// 돌이 날아가는 동안, 돌이 곧 지나갈 자리 근처 수면에서 물고기가 튀어 오르게 한다.
// 돌로 맞추면 돈을 받고 물고기를 도감에 등록한다.
public class FishSpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    private PlayerController _player;
    [SerializeField]
    private PlayerProgress _progress;
    [SerializeField]
    private SkipEffect _effect;
    [SerializeField]
    private GameObject _water;
    private Rigidbody _playerBody;
    private float _waterY;

    [Header("물고기 종류")]
    [Tooltip("출현하는 물고기 프리팹. 이 순서대로 던질 거리 선택 목록에 나온다")]
    [SerializeField]
    private Fish[] _fishPrefabs;
    private FishType[] _fishTypes;

    [Header("출현")]
    [Tooltip("물고기가 튀어 오르는 간격 범위(초)")]
    [SerializeField]
    private Vector2 _spawnIntervalRange = new Vector2(0.35f, 0.9f);
    [Tooltip("한 번에 튀어 오르는 마릿수 범위")]
    [SerializeField]
    private Vector2Int _schoolSizeRange = new Vector2Int(1, 3);
    [Tooltip("몇 초 뒤 돌이 지나갈 자리에서 물고기가 최고점에 오를지")]
    [SerializeField]
    private Vector2 _leadTimeRange = new Vector2(1f, 2f);
    [Tooltip("돌 진행 경로에서 옆으로 벗어나는 최대 거리. A/D로 맞추러 갈 수 있는 정도")]
    [SerializeField]
    private float _maxSideOffset = 9f;
    [SerializeField]
    private Vector2 _jumpHeightRange = new Vector2(3f, 8f);
    [Tooltip("물고기가 돌 경로를 가로지르는 속도 범위")]
    [SerializeField]
    private Vector2 _crossSpeedRange = new Vector2(2f, 5f);
    [SerializeField]
    private int _maxFish = 16;
    [Tooltip("돌이 이보다 느리면 물고기가 나오지 않는다")]
    [SerializeField]
    private float _minStoneSpeed = 10f;
    [SerializeField]
    private Color _catchFlashColor = new Color(1f, 0.85f, 0.3f, 0.3f);
    [Tooltip("복어가 부풀 때 돌을 위로 튕겨 올리는 속도")]
    [SerializeField]
    private float _pufferBounce = 10f;
    private readonly List<Fish> _activeFish = new List<Fish>();
    private float _nextSpawnTime;
    public event Action<FishType> OnFishCaught;

    public Rigidbody PlayerBody => _playerBody;
    public IReadOnlyList<FishType> FishTypes => _fishTypes;

    void Awake()
    {
        _playerBody = _player.GetComponent<Rigidbody>();
        _waterY = _water.GetComponent<Collider>().bounds.max.y;
        _fishTypes = new FishType[_fishPrefabs.Length];
        for (int i = 0; i < _fishPrefabs.Length; i++)
        {
            _fishTypes[i] = _fishPrefabs[i].Type;
        }
    }

    void Update()
    {
        if (!_player.IsThrown || _player.IsGameOver || Time.time < _nextSpawnTime) return;

        _nextSpawnTime = Time.time + UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
        if (_player.Speed < _minStoneSpeed) return;

        int schoolSize = UnityEngine.Random.Range(_schoolSizeRange.x, _schoolSizeRange.y + 1);
        for (int i = 0; i < schoolSize && _activeFish.Count < _maxFish; i++)
        {
            Spawn();
        }
    }

    /// <summary>
    /// 떠 있는 물고기를 모두 없앤다. 재시작할 때 쓴다.
    /// 입력값은 없으며, _activeFish를 비운다.
    /// </summary>
    public void Clear()
    {
        foreach (Fish fish in _activeFish)
        {
            Destroy(fish.gameObject);
        }
        _activeFish.Clear();
    }

    /// <summary>
    /// 물고기가 수면 위로 튀어 오를 때 물보라를 만든다.
    /// point를 사용하며, 물보라와 물결을 만든다.
    /// </summary>
    public void HandleFishSurfaced(Vector3 point)
    {
        _effect.PlaySplash(point, 0.35f);
    }

    /// <summary>
    /// 물고기가 다시 물에 들어가면 물보라를 만들고 없앤다.
    /// fish를 사용하며, _activeFish에서 제거한다.
    /// </summary>
    public void HandleFishLanded(Fish fish)
    {
        Vector3 point = fish.transform.position;
        point.y = _waterY;
        _effect.PlaySplash(point, 0.3f);
        Remove(fish);
    }

    /// <summary>
    /// 돌에 맞은 물고기를 도감에 등록하고 돈을 주며 화면 효과와 포획 이벤트를 보낸다.
    /// fish를 사용하며, 진행 상황(돈, 도감 등록)을 변경하고 _activeFish에서 제거한다.
    /// </summary>
    public void HandleFishCaught(Fish fish)
    {
        _progress.AddFish(fish.Type);
        _effect.PlaySplash(fish.transform.position, 0.6f);
        _effect.PlayImpact(0.8f, _catchFlashColor);
        OnFishCaught?.Invoke(fish.Type);
        Remove(fish);
    }

    /// <summary>
    /// 복어가 돌에 닿아 부풀기 시작하면 돌을 위로 튕겨 올리고 화면 효과를 준다. 다 부풀면 HandleFishCaught가 불린다.
    /// fish를 사용하며, 돌 속도를 변경한다.
    /// </summary>
    public void HandlePufferTouched(Fish fish)
    {
        _player.AddBounce(_pufferBounce);
        _effect.PlayImpact(0.7f, _catchFlashColor);
    }

    /// <summary>
    /// 돌이 몇 초 뒤 지나갈 자리 근처에서 최고점에 오르도록 물고기 프리팹을 만들어 튀어 오르게 한다.
    /// 돌 위치와 속도, 출현 설정을 사용하며, 새 물고기를 _activeFish에 추가한다.
    /// </summary>
    private void Spawn()
    {
        Fish prefab = PickPrefab();
        Vector3 horizontal = _player.Velocity;
        horizontal.y = 0f;
        Vector3 heading = horizontal.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, heading);

        float lead = UnityEngine.Random.Range(_leadTimeRange.x, _leadTimeRange.y);
        float sideOffset = UnityEngine.Random.Range(-_maxSideOffset, _maxSideOffset);
        Vector3 apex = _playerBody.position + horizontal * lead + side * sideOffset;
        apex.y = _waterY;

        float height = UnityEngine.Random.Range(_jumpHeightRange.x, _jumpHeightRange.y);
        float gravity = -Physics.gravity.y;
        float upSpeed = Mathf.Sqrt(2f * gravity * height);
        float timeToApex = upSpeed / gravity;

        // 돌 경로 쪽으로 가로질러 헤엄치게 해서, 옆으로 벗어난 물고기도 경로를 지나가게 한다.
        float crossSign = sideOffset > 0f ? -1f : 1f;
        Vector3 cross = side * (crossSign * UnityEngine.Random.Range(_crossSpeedRange.x, _crossSpeedRange.y));
        Vector3 start = apex - cross * timeToApex;
        float delay = Mathf.Max(0f, lead - timeToApex);

        Fish fish = Instantiate(prefab);
        fish.Launch(this, start, cross + Vector3.up * upSpeed, delay, _waterY);
        _activeFish.Add(fish);
    }

    /// <summary>
    /// 출현 가중치에 따라 물고기 프리팹을 고른다.
    /// _fishPrefabs 종류의 SpawnWeight를 사용하며, 고른 프리팹을 반환한다.
    /// </summary>
    private Fish PickPrefab()
    {
        float total = 0f;
        foreach (Fish prefab in _fishPrefabs)
        {
            total += prefab.Type.SpawnWeight;
        }

        float pick = UnityEngine.Random.Range(0f, total);
        foreach (Fish prefab in _fishPrefabs)
        {
            pick -= prefab.Type.SpawnWeight;
            if (pick <= 0f) return prefab;
        }
        return _fishPrefabs[_fishPrefabs.Length - 1];
    }

    /// <summary>
    /// 물고기를 목록에서 빼고 오브젝트를 없앤다.
    /// fish를 사용하며, _activeFish를 변경한다.
    /// </summary>
    private void Remove(Fish fish)
    {
        _activeFish.Remove(fish);
        Destroy(fish.gameObject);
    }
}
