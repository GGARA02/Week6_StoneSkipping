using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

// 던진 물고기가 날아가는 동안, 곧 지나갈 자리 근처 수면에서 물고기가 튀어 오르게 한다.
// 던진 물고기로 맞추면 돈을 받고 물고기를 도감에 등록한다.
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
    private Fish _wallFish;
    private Mesh _wallCatalogMesh;
    private CuttableWall[] _walls = Array.Empty<CuttableWall>();

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
    public Fish WallFish => _wallFish;

    void Awake()
    {
        _playerBody = _player.GetComponent<Rigidbody>();
        _waterY = _water.GetComponent<Collider>().bounds.max.y;
        InitializeWalls();
        _fishTypes = new FishType[_fishPrefabs.Length];
        for (int i = 0; i < _fishPrefabs.Length; i++)
        {
            _fishTypes[i] = _fishPrefabs[i].Type;
        }
    }

    void OnDestroy()
    {
        if (_wallFish != null) Destroy(_wallFish.gameObject);
        if (_wallCatalogMesh != null) Destroy(_wallCatalogMesh);
    }

    /// <summary>
    /// 씬의 Wall 자식 큐브와 저장 메시를 사용해 절단 대상과 Wall 선택 항목을 준비한다.
    /// 자연 출현 목록의 끝에 비활성 Wall 템플릿을 추가하고 각 큐브에 절단 동작을 연결한다.
    /// </summary>
    private void InitializeWalls()
    {
        GameObject wallRoot = GameObject.Find("Wall");
        if (wallRoot == null) return;
        MeshFilter[] walls = wallRoot.GetComponentsInChildren<MeshFilter>();
        if (walls.Length == 0) return;

        GameObject template = new GameObject("Wall Fish Catalog");
        template.SetActive(false);
        template.transform.SetParent(transform, false);
        MeshFilter filter = template.AddComponent<MeshFilter>();
        _wallCatalogMesh = _progress.LoadWallMesh();
        filter.sharedMesh = _wallCatalogMesh != null ? _wallCatalogMesh : walls[0].sharedMesh;
        template.AddComponent<MeshRenderer>().sharedMaterials = walls[0].GetComponent<MeshRenderer>().sharedMaterials;
        template.AddComponent<Rigidbody>().isKinematic = true;
        _wallFish = template.AddComponent<Fish>();
        _wallFish.InitializeWall(this, FishType.CreateWall(), null);
        int count = _fishPrefabs.Length;
        Array.Resize(ref _fishPrefabs, count + 1);
        _fishPrefabs[count] = _wallFish;
        _walls = new CuttableWall[walls.Length];
        for (int i = 0; i < walls.Length; i++)
        {
            _walls[i] = walls[i].gameObject.AddComponent<CuttableWall>();
            _walls[i].Initialize(this);
        }
    }

    /// <summary>
    /// 초기화 때 보관한 벽 목록을 사용해 모든 장애물을 새 throw의 원본 큐브로 복원한다.
    /// 이전 조각은 제거하지만 도감에 등록된 Wall 메쉬와 해금 상태는 유지한다.
    /// </summary>
    public void ResetWalls()
    {
        foreach (CuttableWall wall in _walls) wall.ResetWall();
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
    /// point와 _fishRippleScale을 사용하며, 물보라와 물결을 만든다.
    /// </summary>
    public void HandleFishSurfaced(Vector3 point)
    {
        _effect.PlaySplash(point, 0.35f, _fishRippleScale);
    }

    /// <summary>
    /// 물고기가 다시 물에 들어가면 물보라를 만들고 없앤다.
    /// fish와 _fishRippleScale을 사용하며, _activeFish에서 제거한다.
    /// </summary>
    public void HandleFishLanded(Fish fish)
    {
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
        if (fish.Type.Id == "wall")
        {
            Mesh previous = _wallCatalogMesh;
            _wallCatalogMesh = _progress.SaveWallMesh(fish.GetComponent<MeshFilter>().sharedMesh, fish.transform.lossyScale);
            _wallFish.GetComponent<MeshFilter>().sharedMesh = _wallCatalogMesh;
            if (previous != null) Destroy(previous);
        }
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
            if (prefab.Type.SpawnWeight <= 0f) continue;
            pick -= prefab.Type.SpawnWeight;
            if (pick <= 0f) return prefab;
        }
        return _fishPrefabs[0];
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
