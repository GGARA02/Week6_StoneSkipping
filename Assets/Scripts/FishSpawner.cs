using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

// 던진 물고기가 날아가는 동안, 곧 지나갈 자리 근처 수면에서 물고기가 튀어 오르게 한다.
// 던진 물고기로 맞추면 돈을 받고 물고기를 도감에 등록한다.
public class FishSpawner : MonoBehaviour
{
    private const string SEA_KEY_PREFIX = "SkipStoneV2.Sea.";

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
    private readonly HashSet<string> _seaTypes = new HashSet<string>();
    private readonly HashSet<string> _pendingSeaTypes = new HashSet<string>();

    [Header("출현")]
    [Tooltip("물고기가 튀어 오르는 간격 범위(초)")]
    [SerializeField]
    private Vector2 _spawnIntervalRange = new Vector2(0.35f, 0.9f);
    [Tooltip("한 번에 튀어 오르는 마릿수 범위")]
    [SerializeField]
    private Vector2Int _schoolSizeRange = new Vector2Int(1, 3);
    [Tooltip("몇 초 뒤 던진 물고기가 지나갈 자리에서 물고기가 최고점에 오를지")]
    [SerializeField]
    private Vector2 _leadTimeRange = new Vector2(1f, 2f);
    [Tooltip("던진 물고기 진행 경로에서 옆으로 벗어나는 최대 거리. A/D로 맞추러 갈 수 있는 정도")]
    [SerializeField]
    private float _maxSideOffset = 9f;
    [SerializeField]
    private Vector2 _jumpHeightRange = new Vector2(3f, 8f);
    [Tooltip("물고기가 던진 물고기 경로를 가로지르는 속도 범위")]
    [SerializeField]
    private Vector2 _crossSpeedRange = new Vector2(2f, 5f);
    [SerializeField]
    private int _maxFish = 16;
    [Tooltip("던진 물고기가 이보다 느리면 물고기가 나오지 않는다")]
    [FormerlySerializedAs("_minStoneSpeed")]
    [SerializeField]
    private float _minFishSpeed = 10f;
    [SerializeField]
    private Color _catchFlashColor = new Color(1f, 0.85f, 0.3f, 0.3f);
    private readonly List<Fish> _activeFish = new List<Fish>();
    private float _nextSpawnTime;
    public event Action<FishType> OnFishCaught;

    [Header("물결")]
    [Tooltip("물고기가 튀어 오르거나 물에 들어갈 때 물결 세기 배율")]
    [SerializeField]
    private float _fishRippleScale = 2.5f;

    public Rigidbody PlayerBody => _playerBody;
    public IReadOnlyList<Fish> FishPrefabs => _fishPrefabs;
    public IReadOnlyList<FishType> FishTypes => _fishTypes;

    void Awake()
    {
        _playerBody = _player.GetComponent<Rigidbody>();
        _waterY = _water.GetComponent<Collider>().bounds.max.y;
        RegisterEjectedPrefabs();
        _fishTypes = new FishType[_fishPrefabs.Length];
        for (int i = 0; i < _fishPrefabs.Length; i++)
        {
            _fishTypes[i] = _fishPrefabs[i].Type;
            if (PlayerPrefs.GetInt(SEA_KEY_PREFIX + _fishTypes[i].Id, 0) > 0)
            {
                _seaTypes.Add(_fishTypes[i].Id);
            }
        }
    }

    void Update()
    {
        if (!_player.IsThrown || _player.IsGameOver || Time.time < _nextSpawnTime) return;

        _nextSpawnTime = Time.time + UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
        if (_player.Speed < _minFishSpeed) return;

        int schoolSize = UnityEngine.Random.Range(_schoolSizeRange.x, _schoolSizeRange.y + 1);
        for (int i = 0; i < schoolSize && _activeFish.Count < _maxFish; i++)
        {
            Spawn();
        }
    }

    /// <summary>
    /// 재시작 시 떠 있는 물고기를 없애고 이번 판의 입수 종류를 다음 판 출현에 반영한다.
    /// 입력값은 없으며, 활성 목록과 대기 기록을 비우고 출현 가능 종류와 시간을 갱신한다.
    /// </summary>
    public void Clear()
    {
        foreach (Fish fish in _activeFish)
        {
            Destroy(fish.gameObject);
        }
        _activeFish.Clear();
        _seaTypes.UnionWith(_pendingSeaTypes);
        _pendingSeaTypes.Clear();
        _nextSpawnTime = Time.time;
    }

    /// <summary>
    /// 물고기가 수면 위로 튀어 오를 때 물보라를 만든다.
    /// point와 _fishRippleScale을 사용하며, 물보라와 물결을 만든다.
    /// </summary>
    public void HandleFishSurfaced(Vector3 point)
    {
        _effect.PlaySplash(point, 0.35f, _fishRippleScale);
    }

    /// <summary>
    /// 물고기가 입수하면 물보라를 만들고 사출물의 바다 등록을 저장한 뒤 없앤다.
    /// fish와 _fishRippleScale을 사용하며, 다음 판 출현 기록과 PlayerPrefs, 활성 목록을 변경한다.
    /// </summary>
    public void HandleFishLanded(Fish fish)
    {
        if (fish.IsEjected)
        {
            _pendingSeaTypes.Add(fish.Type.Id);
            PlayerPrefs.SetInt(SEA_KEY_PREFIX + fish.Type.Id, 1);
            PlayerPrefs.Save();
        }
        Vector3 point = fish.transform.position;
        point.y = _waterY;
        _effect.PlaySplash(point, 0.3f, _fishRippleScale);
        Remove(fish);
    }

    /// <summary>
    /// 던진 물고기에 맞은 물고기를 도감에 등록하고 돈을 주며 화면 효과와 포획 이벤트를 보낸다.
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
    /// 던진 물고기가 몇 초 뒤 지나갈 자리 근처에서 최고점에 오르도록 물고기 프리팹을 만들어 튀어 오르게 한다.
    /// 던진 물고기 위치와 속도, 출현 설정을 사용하며, 새 물고기를 _activeFish에 추가한다.
    /// </summary>
    private void Spawn()
    {
        Fish prefab = PickPrefab();
        if (prefab == null) return;
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

        // 던진 물고기 경로 쪽으로 가로질러 헤엄치게 해서, 옆으로 벗어난 물고기도 경로를 지나가게 한다.
        float crossSign = sideOffset > 0f ? -1f : 1f;
        Vector3 cross = side * (crossSign * UnityEngine.Random.Range(_crossSpeedRange.x, _crossSpeedRange.y));
        Vector3 start = apex - cross * timeToApex;
        float delay = Mathf.Max(0f, lead - timeToApex);

        Fish fish = Instantiate(prefab);
        FishAbility ability = fish.GetComponent<FishAbility>();
        if (ability != null) ability.OnWaterSpawn();
        fish.Launch(this, start, cross + Vector3.up * upSpeed, delay, _waterY);
        _activeFish.Add(fish);
    }

    /// <summary>
    /// 출현 가중치에 따라 물고기 프리팹을 고른다.
    /// 종류별 출현 정책과 SpawnWeight를 사용하며, 고른 프리팹 또는 후보가 없으면 null을 반환한다.
    /// </summary>
    private Fish PickPrefab()
    {
        float total = 0f;
        foreach (Fish prefab in _fishPrefabs)
        {
            if (CanSpawn(prefab.Type)) total += Mathf.Max(0f, prefab.Type.SpawnWeight);
        }

        if (total <= 0f) return null;
        float pick = UnityEngine.Random.Range(0f, total);
        foreach (Fish prefab in _fishPrefabs)
        {
            if (!CanSpawn(prefab.Type) || prefab.Type.SpawnWeight <= 0f) continue;
            pick -= prefab.Type.SpawnWeight;
            if (pick < 0f) return prefab;
        }
        return null;
    }

    /// <summary>
    /// 사출 능력에 연결된 프리팹을 종류별로 한 번씩 선택 목록에 추가한다.
    /// _fishPrefabs의 사출 참조를 사용하며, 연결된 2차 프리팹까지 목록을 확장한다.
    /// </summary>
    private void RegisterEjectedPrefabs()
    {
        List<Fish> prefabs = new List<Fish>(_fishPrefabs);
        HashSet<string> ids = new HashSet<string>();
        foreach (Fish prefab in prefabs) ids.Add(prefab.Type.Id);
        for (int i = 0; i < prefabs.Count; i++)
        {
            foreach (EjectFishAbility ability in prefabs[i].GetComponents<EjectFishAbility>())
            {
                Fish childPrefab = ability.FishPrefab;
                if (ids.Add(childPrefab.Type.Id)) prefabs.Add(childPrefab);
            }
        }
        _fishPrefabs = prefabs.ToArray();
    }

    /// <summary>
    /// 종류의 바다 등록, 획득 정책과 동시 출현 제한을 확인한다.
    /// type과 현재 상태를 사용하며, 자연 출현 가능 여부를 반환한다.
    /// </summary>
    private bool CanSpawn(FishType type)
    {
        if (type.RequiresEjection && !_seaTypes.Contains(type.Id)) return false;
        return CanCreate(type);
    }

    /// <summary>
    /// 획득 후 중단 설정과 종류별 활성 개체 수를 확인한다.
    /// type을 사용하며, 사출 또는 출수 개체를 추가할 수 있는지 반환한다.
    /// </summary>
    private bool CanCreate(FishType type)
    {
        if (type.StopAfterCatch && _progress.IsFishRegistered(type)) return false;
        if (type.MaxConcurrent <= 0) return true;
        int count = 0;
        foreach (Fish fish in _activeFish)
        {
            if (fish.Type.Id == type.Id) count++;
        }
        return count < type.MaxConcurrent;
    }

    /// <summary>
    /// 종류별 제한을 만족하면 지정한 위치와 크기로 독립 물고기를 사출한다.
    /// prefab, 위치, 회전, 배율과 velocity를 사용하며, 활성 목록에 추가하고 성공 여부를 반환한다.
    /// </summary>
    public bool TryEject(Fish prefab, Vector3 position, Quaternion rotation, Vector3 scale, Vector3 velocity)
    {
        FishType type = prefab.Type;
        if (!CanCreate(type)) return false;
        if (!type.AllowRepeatEjection)
        {
            if (_seaTypes.Contains(type.Id) || _pendingSeaTypes.Contains(type.Id)
                || _progress.IsFishRegistered(type)) return false;
            foreach (Fish active in _activeFish)
            {
                if (active.Type.Id == type.Id && active.IsEjected) return false;
            }
        }

        Fish fish = Instantiate(prefab, position, rotation);
        fish.transform.localScale = scale;
        FishAbility ability = fish.GetComponent<FishAbility>();
        if (ability != null) velocity = ability.OnEjected(velocity);
        fish.Eject(this, position, velocity, _waterY);
        _activeFish.Add(fish);
        return true;
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
